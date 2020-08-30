using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	/// <summary>
	/// Blocks to instructions converter
	/// </summary>
	public sealed class CodeGenerator {
		private List<BasicBlock> _basicBlocks;

#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		private CodeGenerator() {
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
		}

		/// <summary>
		/// Converts a method block into instructions
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <param name="instructions"></param>
		/// <param name="exceptionHandlers"></param>
		/// <param name="locals"></param>
		public static void Generate(ScopeBlock methodBlock, out IList<Instruction> instructions, out IList<ExceptionHandler> exceptionHandlers, out IList<Local> locals) {
			var generator = new CodeGenerator();
			generator.Layout(methodBlock);
			instructions = generator.GenerateInstructions();
			exceptionHandlers = generator.GenerateExceptionHandlers();
			locals = GenerateLocals((List<Instruction>)instructions);
			generator.Cleanup();
		}

		private void Layout(ScopeBlock methodBlock) {
			var lastProtectedBlocks = new List<ScopeBlock>();
			int index = 0;
			_basicBlocks = new List<BasicBlock>();

			foreach (var block in methodBlock.Enumerate<Block>()) {
				if (block is BasicBlock basicBlock) {
					basicBlock.Contexts.Set(this, new BlockContext(index, basicBlock.BranchOpcode, lastProtectedBlocks));
					_basicBlocks.Add(basicBlock);
					lastProtectedBlocks.Clear();
					index++;
				}
				else if (block.Type == BlockType.Protected) {
					lastProtectedBlocks.Add((ScopeBlock)block);
				}
			}
		}

		private void Cleanup() {
			foreach (var basicBlock in _basicBlocks)
				basicBlock.Contexts.Remove<BlockContext>(this);
		}

		private List<Instruction> GenerateInstructions() {
			int instructionCount = 0;
			foreach (var basicBlock in _basicBlocks) {
				var blockContext = basicBlock.Contexts.Get<BlockContext>(this);
				var branchInstruction = blockContext.BranchInstruction;
				instructionCount += basicBlock.Instructions.Count;
				instructionCount++;

				switch (basicBlock.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.CondBranch: break;
				case FlowControl.Return:
				case FlowControl.Throw: continue;
				default: throw new ArgumentOutOfRangeException(nameof(OpCode.FlowControl));
				}

				var fallThrough = basicBlock.FallThrough;
				if (basicBlock.FlowControl == FlowControl.Branch) {
					branchInstruction.Operand = GetFirstInstruction(fallThrough);
					continue;
				}
				// unconditional branch

				if (basicBlock.FlowControl == FlowControl.CondBranch) {
					if (branchInstruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						branchInstruction.Operand = GetFirstInstruction(basicBlock.CondTarget);
					}
					else if (branchInstruction.OpCode.OperandType == OperandType.InlineSwitch) {
						var switchTargets = basicBlock.SwitchTargets;
						var operand = new Instruction[switchTargets.Count];
						for (int i = 0; i < operand.Length; i++)
							operand[i] = GetFirstInstruction(switchTargets[i]);
						branchInstruction.Operand = operand;
					}
					else {
						throw new InvalidOperationException();
					}
					// Sets conditional branch
					if (!IsNextBasicBlock(fallThrough, blockContext.Index)) {
						blockContext.FixupInstruction = OpCodes.Br.ToInstruction(GetFirstInstruction(fallThrough));
						instructionCount++;
					}
					// Checks whether fall through fixup should be added.
				}
				// conditional branch
			}

			var instructions = new List<Instruction>(instructionCount);
			foreach (var basicBlock in _basicBlocks) {
				var blockContext = basicBlock.Contexts.Get<BlockContext>(this);

				instructions.AddRange(basicBlock.Instructions);
				if (basicBlock.IsEmpty || basicBlock.BranchOpcode.Code != Code.Br
					|| !IsNextBasicBlock(basicBlock.FallThrough, blockContext.Index))
					instructions.Add(blockContext.BranchInstruction);
				// If basic block is empty, we should preserve at least one instruction so we can locate this block.
				// If branch opcode is br and fall through is next basic block, we can skip writing br instruction.
				if (!(blockContext.FixupInstruction is null))
					instructions.Add(blockContext.FixupInstruction);
				// If it is conditional branch and fall through is not next basic block, we should add a br instruction.
			}

#if DEBUG
			Debug.Assert(instructions.Capacity == instructionCount);
#endif

			return instructions;
		}

		private List<ExceptionHandler> GenerateExceptionHandlers() {
			var exceptionHandlers = new List<ExceptionHandler>();
			for (int i = _basicBlocks.Count - 1; i >= 0; i--) {
				// The innermost exception block should be declared first. (error: 0x801318A4)
				var basicBlock = _basicBlocks[i];
				var protectedBlocks = basicBlock.Contexts.Get<BlockContext>(this).ProtectedBlocks;
				if (protectedBlocks is null || protectedBlocks.Count == 0)
					continue;

				for (int j = protectedBlocks.Count - 1; j >= 0; j--) {
					var protectedBlock = protectedBlocks[j];
					var blocks = protectedBlock.Blocks;

					int tryBlockIndex = -1;
					for (int k = 0; k < blocks.Count; k++) {
						if (blocks[k].Type == BlockType.Try) {
							tryBlockIndex = k;
							break;
						}
					}
					var tryBlock = (ScopeBlock)blocks[tryBlockIndex];
					var tryStartBlock = tryBlock.FirstBlock.First();
					var tryEndBlock = GetNextBasicBlock(tryBlock.LastBlock.Last());
					var tryStart = GetFirstInstruction(tryStartBlock);
					var tryEnd = !(tryEndBlock is null) ? GetFirstInstruction(tryEndBlock) : null;

					for (int k = 0; k < blocks.Count; k++) {
						if (k == tryBlockIndex) {
							k++;
							if (k == blocks.Count)
								break;
						}
						if (blocks[k].Type == BlockType.Filter)
							exceptionHandlers.Add(GetExceptionHandler(tryStart, tryEnd, (ScopeBlock)blocks[k], (HandlerBlock)blocks[++k]));
						else
							exceptionHandlers.Add(GetExceptionHandler(tryStart, tryEnd, null, (HandlerBlock)blocks[k]));
					}
				}
			}
			return exceptionHandlers;
		}

		private ExceptionHandler GetExceptionHandler(Instruction tryStart, Instruction? tryEnd, ScopeBlock? filterBlock, HandlerBlock handlerBlock) {
			var filterStart = filterBlock?.FirstBlock.First();
			var handlerStart = handlerBlock.FirstBlock.First();
			var handlerEnd = GetNextBasicBlock(handlerBlock.LastBlock.Last());

			return new ExceptionHandler() {
				TryStart = tryStart,
				TryEnd = tryEnd,
				HandlerStart = GetFirstInstruction(handlerStart),
				HandlerEnd = !(handlerEnd is null) ? GetFirstInstruction(handlerEnd) : null,
				FilterStart = !(filterStart is null) ? GetFirstInstruction(filterStart) : null,
				CatchType = handlerBlock.CatchType,
				HandlerType = GetHandlerType(filterBlock, handlerBlock)
			};
		}

		private static List<Local> GenerateLocals(List<Instruction> instructions) {
			var locals = new List<Local>();
			foreach (var instruction in instructions) {
				switch (instruction.OpCode.Code) {
				case Code.Ldloc:
				case Code.Ldloca:
				case Code.Stloc: {
					var local = (Local)instruction.Operand;
					if (local is null)
						Debug.Assert(false);
					else if (!locals.Contains(local))
						locals.Add(local);
					break;
				}
				}
			}
			return locals;
		}

		private bool IsNextBasicBlock(BasicBlock basicBlock, int index) {
			index += 1;
			return index != _basicBlocks.Count && _basicBlocks[index] == basicBlock;
		}

		private BasicBlock? GetNextBasicBlock(BasicBlock basicBlock) {
			int i = basicBlock.Contexts.Get<BlockContext>(this).Index + 1;
			return i != _basicBlocks.Count ? _basicBlocks[i] : null;
		}

		private Instruction GetFirstInstruction(BasicBlock basicBlock) {
			return !basicBlock.IsEmpty ? basicBlock.Instructions[0] : basicBlock.Contexts.Get<BlockContext>(this).BranchInstruction;
		}

		private static ExceptionHandlerType GetHandlerType(ScopeBlock? filterBlock, HandlerBlock handlerBlock) {
			return !(filterBlock is null) ? ExceptionHandlerType.Filter : handlerBlock.Type switch
			{
				BlockType.Catch => ExceptionHandlerType.Catch,
				BlockType.Finally => ExceptionHandlerType.Finally,
				BlockType.Fault => ExceptionHandlerType.Fault,
				_ => throw new InvalidOperationException(),
			};
		}

		private sealed class BlockContext : IBlockContext {
			public readonly int Index;
			public readonly Instruction BranchInstruction;
			public Instruction? FixupInstruction;
			public List<ScopeBlock>? ProtectedBlocks;

			public BlockContext(int index, OpCode branchOpcode, List<ScopeBlock> protectedBlocks) {
				Index = index;
				BranchInstruction = new Instruction(branchOpcode);
				if (protectedBlocks.Count != 0)
					ProtectedBlocks = new List<ScopeBlock>(protectedBlocks);
#if DEBUG
				BranchInstruction.Offset = 0xF000 | (uint)index;
#endif
			}
		}
	}
}
