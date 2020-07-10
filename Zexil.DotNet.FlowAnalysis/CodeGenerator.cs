using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
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
		public static void Generate(MethodBlock methodBlock, out IList<Instruction> instructions, out IList<ExceptionHandler> exceptionHandlers, out IList<Local> locals) {
			var generator = new CodeGenerator();
			generator.Layout(methodBlock);
			instructions = generator.GenerateInstructions();
			exceptionHandlers = generator.GenerateExceptionHandlers();
			locals = GenerateLocals((List<Instruction>)instructions);
			generator.Cleanup();
		}

		private void Layout(MethodBlock methodBlock) {
			var lastTryBlocks = new List<TryBlock>();
			int index = 0;
			_basicBlocks = new List<BasicBlock>();

			BlockVisitor.VisitAll(methodBlock, onBlockEnter: block => {
				if (block is BasicBlock basicBlock) {
					basicBlock.Contexts.Set(this, new BlockContext(index, basicBlock.BranchOpcode, lastTryBlocks));
					_basicBlocks.Add(basicBlock);
					lastTryBlocks.Clear();
					index++;
				}
				else if (block is TryBlock tryBlock) {
					lastTryBlocks.Add(tryBlock);
				}
				return false;
			});
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

				switch (branchInstruction.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Cond_Branch: break;
				case FlowControl.Return:
				case FlowControl.Throw: continue;
				default: throw new ArgumentOutOfRangeException(nameof(OpCode.FlowControl));
				}

				var fallThrough = basicBlock.FallThrough;
				if (branchInstruction.OpCode.FlowControl == FlowControl.Branch) {
					branchInstruction.Operand = GetFirstInstruction(fallThrough);
					continue;
				}
				// unconditional branch

				if (branchInstruction.OpCode.FlowControl == FlowControl.Cond_Branch) {
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
				var tryBlocks = basicBlock.Contexts.Get<BlockContext>(this).TryBlocks;
				if (tryBlocks is null || tryBlocks.Count == 0)
					continue;

				for (int j = tryBlocks.Count - 1; j >= 0; j--) {
					var tryBlock = tryBlocks[j];
					foreach (var handlerBlock in tryBlock.Handlers)
						exceptionHandlers.Add(GetExceptionHandler(tryBlock, handlerBlock));
				}
			}
			return exceptionHandlers;
		}

		private ExceptionHandler GetExceptionHandler(TryBlock tryBlock, HandlerBlock handlerBlock) {
			var tryStart = tryBlock.FirstBlock.First();
			var tryEnd = GetNextBasicBlock(tryBlock.LastBlock.Last()) ?? throw new InvalidOperationException();
			var filterStart = handlerBlock.Filter?.FirstBlock.First();
			var handlerStart = handlerBlock.FirstBlock.First();
			var handlerEnd = GetNextBasicBlock(handlerBlock.LastBlock.Last());

			return new ExceptionHandler() {
				TryStart = GetFirstInstruction(tryStart),
				TryEnd = GetFirstInstruction(tryEnd),
				HandlerStart = GetFirstInstruction(handlerStart),
				HandlerEnd = !(handlerEnd is null) ? GetFirstInstruction(handlerEnd) : null,
				FilterStart = !(filterStart is null) ? GetFirstInstruction(filterStart) : null,
				CatchType = handlerBlock.CatchType,
				HandlerType = GetHandlerType(handlerBlock)
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

		private static ExceptionHandlerType GetHandlerType(HandlerBlock handlerBlock) {
			return !(handlerBlock.Filter is null) ? ExceptionHandlerType.Filter : handlerBlock.Type switch
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
			public List<TryBlock>? TryBlocks;

			public BlockContext(int index, OpCode branchOpcode, List<TryBlock> tryBlocks) {
				Index = index;
				BranchInstruction = new Instruction(branchOpcode);
				if (tryBlocks.Count != 0)
					TryBlocks = new List<TryBlock>(tryBlocks);
#if DEBUG
				BranchInstruction.Offset = 0xF000 | (uint)index;
#endif
			}
		}
	}
}
