using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Instructions to blocks converter
	/// </summary>
	public sealed class CodeParser {
		private readonly IList<Instruction> _instructions;
		private readonly IList<ExceptionHandler> _exceptionHandlers;

		private CodeParser(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			_instructions = instructions;
			_exceptionHandlers = exceptionHandlers;
		}

		/// <summary>
		/// Converts instructions into a method block
		/// NOTICE: Please call <see cref="CilBody.SimplifyMacros"/> first!
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="exceptionHandlers"></param>
		/// <returns></returns>
		public static MethodBlock Parse(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			if (instructions is null)
				throw new ArgumentNullException(nameof(instructions));
			if (exceptionHandlers is null)
				throw new ArgumentNullException(nameof(exceptionHandlers));
			if (HasNotSupportedInstruction(instructions))
				throw new NotSupportedException("Contains unsupported instruction(s).");

			return new CodeParser(instructions, exceptionHandlers).Parse();
		}

		private MethodBlock Parse() {
			bool[] isEntrys = AnalyzeEntrys(out int entryCount);
			var basicBlocks = CreateBasicBlocks(isEntrys, entryCount);
			var methodBlock = CreateMethodBlock(basicBlocks);
			_instructions.UpdateInstructionOffsets();
			return methodBlock;
		}

		private static bool HasNotSupportedInstruction(IEnumerable<Instruction> instructions) {
			foreach (var instruction in instructions) {
				if (instruction.OpCode.Code == Code.Jmp)
					return true;
			}
			return false;
		}

		private bool[] AnalyzeEntrys(out int entryCount) {
			var instructions = _instructions;
			for (int i = 0; i < instructions.Count; i++)
				instructions[i].Offset = (uint)i;
			// Sets index map
			bool[] isEntrys = new bool[instructions.Count];

			isEntrys[0] = true;
			for (int i = 0; i < instructions.Count; i++) {
				var instruction = instructions[i];
				switch (instruction.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Cond_Branch:
				case FlowControl.Return:
				case FlowControl.Throw: {
					if (i + 1 != instructions.Count) {
						// If current instruction is not the last, then next instruction is a new entry
						isEntrys[i + 1] = true;
					}
					if (instruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						// branch
						isEntrys[(int)((Instruction)instruction.Operand).Offset] = true;
					}
					else if (instruction.OpCode.OperandType == OperandType.InlineSwitch) {
						// switch
						foreach (var target in (IEnumerable<Instruction>)instruction.Operand)
							isEntrys[(int)target.Offset] = true;
					}
					break;
				}
				}
			}

			foreach (var exceptionHandler in _exceptionHandlers) {
				isEntrys[(int)exceptionHandler.TryStart.Offset] = true;
				if (!(exceptionHandler.TryEnd is null))
					isEntrys[(int)exceptionHandler.TryEnd.Offset] = true;
				// try
				if (!(exceptionHandler.FilterStart is null))
					isEntrys[(int)exceptionHandler.FilterStart.Offset] = true;
				// filter
				isEntrys[(int)exceptionHandler.HandlerStart.Offset] = true;
				if (!(exceptionHandler.HandlerEnd is null))
					isEntrys[(int)exceptionHandler.HandlerEnd.Offset] = true;
				// handler
			}

			entryCount = 0;
			for (int i = 0; i < isEntrys.Length; i++) {
				if (isEntrys[i]) {
					instructions[i].Offset = (uint)entryCount;
					entryCount++;
				}
			}

			return isEntrys;
		}

		private BasicBlock[] CreateBasicBlocks(bool[] isEntrys, int entryCount) {
			var basicBlocks = new BasicBlock[entryCount];
			int blockLength = 0;
			for (int i = isEntrys.Length - 1; i >= 0; i--) {
				blockLength++;
				if (!isEntrys[i])
					continue;

				basicBlocks[--entryCount] = new BasicBlock(EnumerateInstructions(_instructions, i, blockLength));
				blockLength = 0;
				basicBlocks[entryCount].Instructions[0].Offset = (uint)entryCount;
			}
			// Creates basic blocks and sets index map for blocks

			for (int i = 0; i < basicBlocks.Length; i++) {
				var basicBlock = basicBlocks[i];
				var instructions = basicBlock.Instructions;
				int lastInstructionIndex = instructions.Count - 1;
				var lastInstruction = instructions[lastInstructionIndex];
				switch (lastInstruction.OpCode.FlowControl) {
				case FlowControl.Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FallThroughNoThrow = basicBlocks[(int)((Instruction)lastInstruction.Operand).Offset];
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				case FlowControl.Cond_Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					if (i + 1 == basicBlocks.Length)
						throw new InvalidMethodException();
					basicBlock.FallThroughNoThrow = basicBlocks[i + 1];
					if (lastInstruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						// branch
						basicBlock.CondTargetNoThrow = basicBlocks[(int)((Instruction)lastInstruction.Operand).Offset];
					}
					else if (lastInstruction.OpCode.OperandType == OperandType.InlineSwitch) {
						// switch
						var switchTargets = (Instruction[])lastInstruction.Operand;
						basicBlock.SwitchTargetsNoThrow = new TargetList(switchTargets.Length);
						for (int j = 0; j < switchTargets.Length; j++)
							basicBlock.SwitchTargetsNoThrow.Add(basicBlocks[(int)switchTargets[j].Offset]);
					}
					else {
						throw new InvalidOperationException();
					}
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				case FlowControl.Call:
				case FlowControl.Next: {
					basicBlock.BranchOpcode = OpCodes.Br;
					if (i + 1 == basicBlocks.Length)
						throw new InvalidMethodException();
					basicBlock.FallThroughNoThrow = basicBlocks[i + 1];
					break;
				}
				case FlowControl.Return:
				case FlowControl.Throw: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				}
			}

			return basicBlocks;

			static IEnumerable<Instruction> EnumerateInstructions(IList<Instruction> instructions, int startIndex, int length) {
				for (int i = startIndex; i < startIndex + length; i++)
					yield return instructions[i];
			}
		}

		private MethodBlock CreateMethodBlock(BasicBlock[] basicBlocks) {
			var exceptionHandlers = _exceptionHandlers;
			if (exceptionHandlers.Count == 0) {
				var methodBlock = new MethodBlock(basicBlocks);
				foreach (var basicBlock in basicBlocks)
					basicBlock.ScopeNoThrow = methodBlock;
				return methodBlock;
			}
			else {
				var blocks = new Block[basicBlocks.Length];
				Array.Copy(basicBlocks, blocks, blocks.Length);
				var ehNodes = CreateEHNodes(exceptionHandlers);
				FoldEHBlocks(blocks, ehNodes);
				var methodBlock = new MethodBlock(EnumerateNonNullBlocks(blocks, 0, blocks.Length));
				SetBlockScope(methodBlock.Blocks, methodBlock);
				return methodBlock;
			}
		}

		private static List<EHNode> CreateEHNodes(IList<ExceptionHandler> exceptionHandlers) {
			var ehNodes = new List<EHNode>();
			foreach (var exceptionHandler in exceptionHandlers) {
				var owner = default(EHNode);
				foreach (var node in ehNodes) {
					if (!IsTryEqual(node.Values[0], exceptionHandler))
						continue;
					owner = node;
					break;
				}

				if (owner is null)
					ehNodes.Add(new EHNode(exceptionHandler));
				else
					owner.Values.Add(exceptionHandler);
			}

			foreach (var ehNode in ehNodes) {
				int start = int.MaxValue;
				int end = int.MinValue;
				foreach (var value in ehNode.Values) {
					int temp = (int)Math.Min(value.TryStart.Offset, value.HandlerType == ExceptionHandlerType.Filter ? value.FilterStart.Offset : value.HandlerStart.Offset);
					start = Math.Min(start, temp);
					temp = (int)Math.Max(value.TryEnd.Offset, value.HandlerEnd.Offset);
					end = Math.Max(end, temp);
				}
				ehNode.Start = start;
				ehNode.End = end;
			}

			ehNodes.Sort((x, y) => {
				int a = x.Start - y.Start;
				if (a != 0)
					return a;
				a = y.End - x.End;
				if (a != 0)
					return a;
				throw new InvalidMethodException();
			});

			return ehNodes;

			static bool IsTryEqual(ExceptionHandler x, ExceptionHandler y) {
				return x.TryStart == y.TryStart && x.TryEnd == y.TryEnd;
			}
		}

		private static void FoldEHBlocks(Block?[] blocks, List<EHNode> ehNodes) {
			for (int i = ehNodes.Count - 1; i >= 0; i--) {
				var ehNode = ehNodes[i];
				var tryValue = ehNode.Values[0];
				var tryBlock = new TryBlock(EnumerateNonNullBlocks(blocks, (int)tryValue.TryStart.Offset, (int)tryValue.TryEnd.Offset));
				RemoveBlocks(blocks, (int)tryValue.TryStart.Offset, (int)tryValue.TryEnd.Offset);
				blocks[(int)tryValue.TryStart.Offset] = tryBlock;

				foreach (var value in ehNode.Values) {
					AddHandler(blocks, tryBlock, value);
					RemoveBlocks(blocks, !(value.FilterStart is null) ? (int)value.FilterStart.Offset : (int)value.HandlerStart.Offset, (int)value.HandlerEnd.Offset);
				}
			}
		}

		private static void RemoveBlocks(Block?[] blocks, int startIndex, int endIndex) {
			for (int i = startIndex; i < endIndex; i++)
				blocks[i] = null;
		}

		private static void AddHandler(Block?[] blocks, TryBlock tryBlock, ExceptionHandler exceptionHandler) {
			var filterBlock = default(FilterBlock);
			if (!(exceptionHandler.FilterStart is null))
				filterBlock = new FilterBlock(EnumerateNonNullBlocks(blocks, (int)exceptionHandler.FilterStart.Offset, (int)exceptionHandler.HandlerStart.Offset));

			var handlerType = exceptionHandler.HandlerType switch
			{
				ExceptionHandlerType.Catch => BlockType.Catch,
				ExceptionHandlerType.Filter => BlockType.Filter,
				ExceptionHandlerType.Finally => BlockType.Finally,
				ExceptionHandlerType.Fault => BlockType.Fault,
				_ => throw new ArgumentOutOfRangeException(),
			};
			var handlerBlock = new HandlerBlock(
				EnumerateNonNullBlocks(blocks, (int)exceptionHandler.HandlerStart.Offset, (int)exceptionHandler.HandlerEnd.Offset),
				handlerType,
				filterBlock,
				exceptionHandler.CatchType
			);
			tryBlock.Handlers.Add(handlerBlock);
		}

		private static IEnumerable<Block> EnumerateNonNullBlocks(Block?[] blocks, int startIndex, int endIndex) {
			for (int i = startIndex; i < endIndex; i++) {
				var block = blocks[i];
				if (!(block is null))
					yield return block;
			}
		}

		private static void SetBlockScope(IEnumerable<Block> blocks, ScopeBlock parent) {
			foreach (var block in blocks) {
				if (block is BasicBlock) {
					block.ScopeNoThrow = parent;
				}
				else if (block is TryBlock tryBlock) {
					tryBlock.ScopeNoThrow = parent;
					SetBlockScope(tryBlock.Blocks, tryBlock);
					SetBlockScope(tryBlock.Handlers, tryBlock);
				}
				else if (block is HandlerBlock handlerBlock) {
					handlerBlock.ScopeNoThrow = parent;
					SetBlockScope(handlerBlock.Blocks, handlerBlock);

					var filterBlock = handlerBlock.Filter;
					if (!(filterBlock is null)) {
						filterBlock.ScopeNoThrow = handlerBlock;
						SetBlockScope(filterBlock.Blocks, handlerBlock);
					}
				}
				else {
					throw new InvalidOperationException();
				}
			}
		}

		private sealed class EHNode {
			public readonly List<ExceptionHandler> Values;
			public int Start;
			public int End;

			public EHNode(ExceptionHandler value) {
				Values = new List<ExceptionHandler> { value };
			}
		}
	}
}
