using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	internal static class CodeGenerator {
		public static void Generate(MethodBlock methodBlock, out IList<Instruction> instructions, out IList<ExceptionHandler> exceptionHandlers, out IList<Local> locals) {
			var basicBlocks = Layout(methodBlock);
			instructions = GenerateInstructions(basicBlocks);
			exceptionHandlers = GenerateExceptionHandlers(basicBlocks);
			locals = GenerateLocals((List<Instruction>)instructions);
			Cleanup(basicBlocks);
		}

		private static List<BasicBlock> Layout(MethodBlock methodBlock) {
			var basicBlocks = new List<BasicBlock>();
			var lastTryBlocks = new List<TryBlock>();
			int index = 0;

			BlockVisitor.Visit(methodBlock, onBlockEnter: block => {
				if (block is BasicBlock basicBlock) {
					basicBlock.Contexts.Add(new BlockContext(index, basicBlock.BranchOpcode, lastTryBlocks));
					basicBlocks.Add(basicBlock);
					lastTryBlocks.Clear();
					index++;
				}
				else if (block is TryBlock tryBlock) {
					lastTryBlocks.Add(tryBlock);
				}
				return false;
			});

			return basicBlocks;
		}

		private static void Cleanup(List<BasicBlock> basicBlocks) {
			foreach (var basicBlock in basicBlocks)
				basicBlock.Contexts.Remove<BlockContext>();
		}

		private static List<Instruction> GenerateInstructions(List<BasicBlock> basicBlocks) {
			int instructionCount = 0;
			foreach (var basicBlock in basicBlocks) {
				var blockContext = basicBlock.Contexts.Peek<BlockContext>();
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

				var fallThroughTarget = basicBlock.FallThroughTarget;
				if (fallThroughTarget is null)
					throw new InvalidOperationException();

				if (branchInstruction.OpCode.FlowControl == FlowControl.Branch) {
					branchInstruction.Operand = GetFirstInstruction(fallThroughTarget);
					continue;
				}
				// unconditional branch

				if (branchInstruction.OpCode.FlowControl == FlowControl.Cond_Branch) {
					if (branchInstruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						var conditionalTarget = basicBlock.ConditionalTarget;
						if (conditionalTarget is null)
							throw new InvalidOperationException();

						branchInstruction.Operand = GetFirstInstruction(conditionalTarget);
					}
					else if (branchInstruction.OpCode.OperandType == OperandType.InlineSwitch) {
						var switchTargets = basicBlock.SwitchTargets;
						if (switchTargets is null)
							throw new InvalidOperationException();

						var operand = new Instruction[switchTargets.Count];
						for (int i = 0; i < operand.Length; i++)
							operand[i] = GetFirstInstruction(switchTargets[i]);
						branchInstruction.Operand = operand;
					}
					else {
						throw new InvalidOperationException();
					}
					// Sets conditional branch
					if (!IsNextBasicBlock(basicBlocks, fallThroughTarget, blockContext.Index)) {
						blockContext.FixupInstruction = OpCodes.Br.ToInstruction(GetFirstInstruction(fallThroughTarget));
						instructionCount++;
					}
					// Checks whether fallthrough fixup should be added.
				}
				// conditional branch
			}

			var instructions = new List<Instruction>(instructionCount);
			for (int i = 0; i < basicBlocks.Count; i++) {
				var basicBlock = basicBlocks[i];
				var blockContext = basicBlock.Contexts.Peek<BlockContext>();

				instructions.AddRange(basicBlock.Instructions);
				if (basicBlock.IsEmpty || basicBlock.BranchOpcode.Code != Code.Br
					|| !IsNextBasicBlock(basicBlocks, basicBlock.FallThroughTarget ?? throw new InvalidOperationException(), blockContext.Index))
					instructions.Add(blockContext.BranchInstruction);
				if (!(blockContext.FixupInstruction is null))
					instructions.Add(blockContext.FixupInstruction);
			}

#if DEBUG
			Debug.Assert(instructions.Capacity == instructionCount);
#endif

			return instructions;
		}

		private static List<ExceptionHandler> GenerateExceptionHandlers(List<BasicBlock> basicBlocks) {
			var exceptionHandlers = new List<ExceptionHandler>();
			for (int i = basicBlocks.Count - 1; i >= 0; i--) {
				// The innermost exception block should be declared first. (error: 0x801318A4)
				var basicBlock = basicBlocks[i];
				var tryBlocks = basicBlock.Contexts.Peek<BlockContext>().TryBlocks;
				if (tryBlocks is null || tryBlocks.Count == 0)
					continue;

				for (int j = tryBlocks.Count - 1; j >= 0; j--) {
					var tryBlock = tryBlocks[j];
					foreach (var handlerBlock in tryBlock.Handlers)
						exceptionHandlers.Add(GetExceptionHandler(basicBlocks, tryBlock, handlerBlock));
				}
			}
			return exceptionHandlers;
		}

		private static ExceptionHandler GetExceptionHandler(List<BasicBlock> basicBlocks, TryBlock tryBlock, HandlerBlock handlerBlock) {
			var tryStart = tryBlock.FirstBlock.GetFirstBasicBlock();
			var tryEnd = GetNextBasicBlock(basicBlocks, tryBlock.LastBlock.GetLastBasicBlock()) ?? throw new InvalidOperationException();
			var filterStart = handlerBlock.Filter?.FirstBlock.GetFirstBasicBlock();
			var handlerStart = handlerBlock.FirstBlock.GetFirstBasicBlock();
			var handlerEnd = GetNextBasicBlock(basicBlocks, handlerBlock.LastBlock.GetLastBasicBlock());

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

		private static bool IsNextBasicBlock(List<BasicBlock> basicBlocks, BasicBlock basicBlock, int index) {
			index += 1;
			return index != basicBlocks.Count ? basicBlocks[index] == basicBlock : false;
		}

		private static BasicBlock? GetNextBasicBlock(List<BasicBlock> basicBlocks, BasicBlock basicBlock) {
			int i = basicBlock.Contexts.Peek<BlockContext>().Index + 1;
			return i != basicBlocks.Count ? basicBlocks[i] : null;
		}

		private static Instruction GetFirstInstruction(BasicBlock basicBlock) {
			return !basicBlock.IsEmpty ? basicBlock.Instructions[0] : basicBlock.Contexts.Peek<BlockContext>().BranchInstruction;
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
