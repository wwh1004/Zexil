using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;
using DNE = dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Emit {
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
		public static ScopeBlock Parse(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			if (instructions is null)
				throw new ArgumentNullException(nameof(instructions));
			if (exceptionHandlers is null)
				throw new ArgumentNullException(nameof(exceptionHandlers));
			if (HasNotSupportedInstruction(instructions))
				throw new NotSupportedException("Contains unsupported instruction(s).");

			return new CodeParser(instructions, exceptionHandlers).Parse();
		}

		private ScopeBlock Parse() {
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
				case DNE.FlowControl.Branch:
				case DNE.FlowControl.Cond_Branch:
				case DNE.FlowControl.Return:
				case DNE.FlowControl.Throw: {
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
#if DEBUG
			_instructions.UpdateInstructionOffsets();
#endif
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
				case DNE.FlowControl.Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FlowControl = FlowControl.Branch;
					basicBlock.FallThroughNoThrow = basicBlocks[(int)((Instruction)lastInstruction.Operand).Offset];
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				case DNE.FlowControl.Cond_Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FlowControl = FlowControl.CondBranch;
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
				case DNE.FlowControl.Call:
				case DNE.FlowControl.Next: {
					basicBlock.BranchOpcode = OpCodes.Br;
					basicBlock.FlowControl = FlowControl.Branch;
					if (i + 1 == basicBlocks.Length)
						throw new InvalidMethodException();
					basicBlock.FallThroughNoThrow = basicBlocks[i + 1];
					break;
				}
				case DNE.FlowControl.Return: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FlowControl = FlowControl.Return;
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				case DNE.FlowControl.Throw: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FlowControl = FlowControl.Throw;
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				default:
					throw new InvalidOperationException();
				}
			}

			return basicBlocks;

			static IEnumerable<Instruction> EnumerateInstructions(IList<Instruction> instructions, int startIndex, int length) {
				for (int i = startIndex; i < startIndex + length; i++)
					yield return instructions[i];
			}
		}

		private ScopeBlock CreateMethodBlock(BasicBlock[] basicBlocks) {
			var exceptionHandlers = _exceptionHandlers;
			if (exceptionHandlers.Count == 0) {
				var methodBlock = new ScopeBlock(basicBlocks, BlockType.Method);
				foreach (var basicBlock in basicBlocks)
					basicBlock.ScopeNoThrow = methodBlock;
				return methodBlock;
			}
			else {
				var blocks = new Block[basicBlocks.Length];
				Array.Copy(basicBlocks, blocks, blocks.Length);
				var ehNodes = CreateEHNodes(exceptionHandlers, blocks.Length);
				FoldEHBlocks(blocks, ehNodes);
				var methodBlock = new ScopeBlock(EnumerateNonNullBlocks(blocks, 0, blocks.Length), BlockType.Method);
				SetBlockScope(methodBlock.Blocks, methodBlock);
				return methodBlock;
			}
		}

		private static List<EHNode> CreateEHNodes(IList<ExceptionHandler> exceptionHandlers, int methodEnd) {
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
					start = Math.Min(start, (int)Math.Min(value.TryStart.Offset, value.HandlerType == ExceptionHandlerType.Filter ? value.FilterStart.Offset : value.HandlerStart.Offset));
					end = Math.Max(end, Math.Max(value.GetTryEndOffset(methodEnd), value.GetHandlerEndOffset(methodEnd)));
				}
				ehNode.Start = start;
				ehNode.End = end;
			}

			return ehNodes;

			static bool IsTryEqual(ExceptionHandler x, ExceptionHandler y) {
				return x.TryStart == y.TryStart && x.TryEnd == y.TryEnd;
			}
		}

		private static void FoldEHBlocks(Block?[] blocks, List<EHNode> ehNodes) {
			for (int i = 0; i < ehNodes.Count; i++) {
				var ehNode = ehNodes[i];
				var tryValue = ehNode.Values[0];
				var protectedBlock = new ScopeBlock(BlockType.Protected);
				var tryBlock = new ScopeBlock(EnumerateNonNullBlocks(blocks, (int)tryValue.TryStart.Offset, tryValue.GetTryEndOffset(blocks.Length)), BlockType.Try);
				protectedBlock.Blocks.Add(tryBlock);
				RemoveBlocks(blocks, (int)tryValue.TryStart.Offset, tryValue.GetTryEndOffset(blocks.Length));
				blocks[(int)tryValue.TryStart.Offset] = protectedBlock;

				foreach (var value in ehNode.Values) {
					AddHandler(blocks, protectedBlock, value);
					RemoveBlocks(blocks, !(value.FilterStart is null) ? (int)value.FilterStart.Offset : (int)value.HandlerStart.Offset, value.GetHandlerEndOffset(blocks.Length));
				}
			}
		}

		private static void RemoveBlocks(Block?[] blocks, int startIndex, int endIndex) {
			for (int i = startIndex; i < endIndex; i++)
				blocks[i] = null;
		}

		private static void AddHandler(Block?[] blocks, ScopeBlock protectedBlock, ExceptionHandler exceptionHandler) {
			if (!(exceptionHandler.FilterStart is null)) {
				var filterBlock = new ScopeBlock(EnumerateNonNullBlocks(blocks, (int)exceptionHandler.FilterStart.Offset, (int)exceptionHandler.HandlerStart.Offset), BlockType.Filter);
				protectedBlock.Blocks.Add(filterBlock);
			}

			var handlerType = exceptionHandler.HandlerType switch
			{
				ExceptionHandlerType.Catch => BlockType.Catch,
				ExceptionHandlerType.Filter => BlockType.Filter,
				ExceptionHandlerType.Finally => BlockType.Finally,
				ExceptionHandlerType.Fault => BlockType.Fault,
				_ => throw new ArgumentOutOfRangeException(),
			};
			var handlerBlock = new HandlerBlock(EnumerateNonNullBlocks(blocks, (int)exceptionHandler.HandlerStart.Offset, exceptionHandler.GetHandlerEndOffset(blocks.Length)), handlerType, exceptionHandler.CatchType);
			protectedBlock.Blocks.Add(handlerBlock);
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
				block.ScopeNoThrow = parent;
				if (block is ScopeBlock scopeBlock)
					SetBlockScope(scopeBlock.Blocks, scopeBlock);
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

	internal static class CodeParserExtensions {
		public static int GetTryEndOffset(this ExceptionHandler exceptionHandler, int methodEnd) {
			return !(exceptionHandler.TryEnd is null) ? (int)exceptionHandler.TryEnd.Offset : methodEnd;
		}

		public static int GetHandlerEndOffset(this ExceptionHandler exceptionHandler, int methodEnd) {
			return !(exceptionHandler.HandlerEnd is null) ? (int)exceptionHandler.HandlerEnd.Offset : methodEnd;
		}
	}
}
