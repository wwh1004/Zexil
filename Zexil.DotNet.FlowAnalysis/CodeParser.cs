using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Instructions to blocks converter
	/// </summary>
	public sealed class CodeParser {
		private readonly IList<Instruction> _instructions;
		private readonly Dictionary<Instruction, int> _instructionMap;
		private readonly IList<ExceptionHandler> _exceptionHandlers;
		private EHInfo[] _ehInfos;
		private int[] _indexRemap;

		private CodeParser(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			_instructions = instructions;
			_instructionMap = Enumerable.Range(0, _instructions.Count).ToDictionary(i => _instructions[i], i => i);
			_exceptionHandlers = exceptionHandlers;
			_ehInfos = Array.Empty<EHInfo>();
			_indexRemap = Array.Empty<int>();
		}

		/// <summary>
		/// Converts instructions into a method block
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
			int entryCount = AnalyzeEntrys();
			var basicBlocks = CreateBasicBlocks(entryCount);
			return CreateMethodBlock(basicBlocks);
		}

		private static bool HasNotSupportedInstruction(IEnumerable<Instruction> instructions) {
			foreach (var instruction in instructions) {
				if (instruction.OpCode.Code == Code.Jmp)
					return true;
			}
			return false;
		}

		private int AnalyzeEntrys() {
			var ehInfos = Enumerable.Range(0, _exceptionHandlers.Count).Select(i => new EHInfo(_exceptionHandlers[i], _instructionMap)).ToArray();
			_ehInfos = ehInfos;
			int[] indexRemap = new int[_instructions.Count];
			_indexRemap = indexRemap;

			indexRemap[0] = 1;
			for (int i = 0; i < _instructions.Count; i++) {
				var instruction = _instructions[i];
				switch (instruction.OpCode.FlowControl) {
				case FlowControl.Branch:
				case FlowControl.Cond_Branch:
				case FlowControl.Return:
				case FlowControl.Throw: {
					if (i + 1 != _instructions.Count) {
						// If current instruction is not the last, then next instruction is a new entry
						indexRemap[i + 1] = 1;
					}
					if (instruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						// branch
						indexRemap[_instructionMap[(Instruction)instruction.Operand]] = 1;
					}
					else if (instruction.OpCode.OperandType == OperandType.InlineSwitch) {
						// switch
						foreach (var target in (IEnumerable<Instruction>)instruction.Operand)
							indexRemap[_instructionMap[target]] = 1;
					}
					break;
				}
				}
			}

			foreach (var ehInfo in ehInfos) {
				indexRemap[ehInfo.TryStart] = 1;
				if (ehInfo.TryEnd != _instructions.Count)
					indexRemap[ehInfo.TryEnd] = 1;
				// try
				if (ehInfo.FilterStart != -1)
					indexRemap[ehInfo.FilterStart] = 1;
				// filter
				indexRemap[ehInfo.HandlerStart] = 1;
				if (ehInfo.HandlerEnd != _instructions.Count)
					indexRemap[ehInfo.HandlerEnd] = 1;
				// handler
			}

			int entryCount = 0;
			for (int i = 0; i < indexRemap.Length; i++) {
				if (indexRemap[i] == 1) {
					indexRemap[i] = entryCount;
					entryCount++;
				}
				else {
					indexRemap[i] = -1;
				}
			}

			return entryCount;
		}

		private BasicBlock[] CreateBasicBlocks(int entryCount) {
			int[] indexRemap = _indexRemap;
			int[] blockLengths = new int[entryCount];
			int blockLength = 0;
			for (int i = indexRemap.Length - 1; i >= 0; i--) {
				blockLength++;
				if (indexRemap[i] == -1)
					continue;

				blockLengths[indexRemap[i]] = blockLength;
				blockLength = 0;
			}

			var basicBlocks = new BasicBlock[blockLengths.Length];
			for (int i = 0; i < indexRemap.Length; i++) {
				int reIndex = indexRemap[i];
				if (reIndex != -1)
					basicBlocks[reIndex] = new BasicBlock(EnumerateInstructions(_instructions, i, blockLengths[reIndex]));
			}

			for (int i = 0; i < indexRemap.Length; i++) {
				int reIndex = indexRemap[i];
				if (reIndex == -1)
					continue;

				var basicBlock = basicBlocks[reIndex];
				basicBlocks[reIndex] = basicBlock;
				var instructions = basicBlock.Instructions;
				int lastInstructionIndex = instructions.Count - 1;
				var lastInstruction = instructions[lastInstructionIndex];
				switch (lastInstruction.OpCode.FlowControl) {
				case FlowControl.Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					basicBlock.FallThroughNoThrow = basicBlocks[indexRemap[_instructionMap[(Instruction)lastInstruction.Operand]]];
					instructions.RemoveAt(lastInstructionIndex);
					break;
				}
				case FlowControl.Cond_Branch: {
					basicBlock.BranchOpcode = lastInstruction.OpCode;
					if (reIndex + 1 == basicBlocks.Length)
						throw new InvalidMethodException();
					basicBlock.FallThroughNoThrow = basicBlocks[reIndex + 1];
					if (lastInstruction.OpCode.OperandType == OperandType.InlineBrTarget) {
						// branch
						basicBlock.CondTargetNoThrow = basicBlocks[indexRemap[_instructionMap[(Instruction)lastInstruction.Operand]]];
					}
					else if (lastInstruction.OpCode.OperandType == OperandType.InlineSwitch) {
						// switch
						var switchTargets = (Instruction[])lastInstruction.Operand;
						basicBlock.SwitchTargetsNoThrow = new TargetList(switchTargets.Length);
						for (int j = 0; j < switchTargets.Length; j++)
							basicBlock.SwitchTargetsNoThrow.Add(basicBlocks[indexRemap[_instructionMap[switchTargets[j]]]]);
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
					if (reIndex + 1 == basicBlocks.Length)
						throw new InvalidMethodException();
					basicBlock.FallThroughNoThrow = basicBlocks[reIndex + 1];
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
			var ehInfos = _ehInfos;
			if (ehInfos.Length == 0) {
				var methodBlock = new MethodBlock(basicBlocks);
				foreach (var basicBlock in basicBlocks)
					basicBlock.ScopeNoThrow = methodBlock;
				return methodBlock;
			}
			else {
				var blocks = new Block[basicBlocks.Length];
				Array.Copy(basicBlocks, blocks, blocks.Length);

				foreach (var ehInfo in ehInfos) {
					ehInfo.TryStart = _indexRemap[ehInfo.TryStart];
					ehInfo.TryEnd = _indexRemap[ehInfo.TryEnd];
					if (ehInfo.FilterStart != -1)
						ehInfo.FilterStart = _indexRemap[ehInfo.FilterStart];
					ehInfo.HandlerStart = _indexRemap[ehInfo.HandlerStart];
					ehInfo.HandlerEnd = ehInfo.HandlerEnd != _instructions.Count ? _indexRemap[ehInfo.HandlerEnd] : basicBlocks.Length;
				}
				var ehNodes = CreateEHNodes(ehInfos);
				FoldEHBlocks(blocks, ehNodes);

				var methodBlock = new MethodBlock(EnumerateNonNullBlocks(blocks, 0, blocks.Length));
				SetBlockScope(methodBlock.Blocks, methodBlock);
				return methodBlock;
			}
		}

		private static List<EHNode> CreateEHNodes(EHInfo[] ehInfos) {
			var ehNodes = new List<EHNode>();
			foreach (var ehInfo in ehInfos) {
				var owner = default(EHNode);
				foreach (var node in ehNodes) {
					if (!node.Infos[0].IsTryEqual(ehInfo))
						continue;
					owner = node;
					break;
				}

				if (owner is null)
					ehNodes.Add(new EHNode(ehInfo));
				else
					owner.Infos.Add(ehInfo);
			}

			foreach (var ehNode in ehNodes) {
				int start = int.MaxValue;
				int end = int.MinValue;
				foreach (var ehInfo in ehNode.Infos) {
					int temp = Math.Min(ehInfo.TryStart, ehInfo.HandlerType == BlockType.Filter ? ehInfo.FilterStart : ehInfo.HandlerStart);
					start = Math.Min(start, temp);
					temp = Math.Max(ehInfo.TryEnd, ehInfo.HandlerEnd);
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
		}

		private static void FoldEHBlocks(Block?[] blocks, List<EHNode> ehNodes) {
			for (int i = ehNodes.Count - 1; i >= 0; i--) {
				var ehNode = ehNodes[i];
				var tryInfo = ehNode.Infos[0];
				var tryBlock = new TryBlock(EnumerateNonNullBlocks(blocks, tryInfo.TryStart, tryInfo.TryEnd));
				RemoveBlocks(blocks, tryInfo.TryStart, tryInfo.TryEnd);
				blocks[tryInfo.TryStart] = tryBlock;

				foreach (var info in ehNode.Infos) {
					AddHandler(blocks, tryBlock, info);
					RemoveBlocks(blocks, info.FilterStart != -1 ? info.FilterStart : info.HandlerStart, info.HandlerEnd);
				}
			}
		}

		private static void RemoveBlocks(Block?[] blocks, int startIndex, int endIndex) {
			for (int i = startIndex; i < endIndex; i++)
				blocks[i] = null;
		}

		private static void AddHandler(Block?[] blocks, TryBlock tryBlock, EHInfo ehInfo) {
			var filterBlock = default(FilterBlock);
			if (ehInfo.FilterStart != -1)
				filterBlock = new FilterBlock(EnumerateNonNullBlocks(blocks, ehInfo.FilterStart, ehInfo.HandlerStart));

			var handlerBlock = new HandlerBlock(
				EnumerateNonNullBlocks(blocks, ehInfo.HandlerStart, ehInfo.HandlerEnd),
				ehInfo.HandlerType,
				filterBlock,
				ehInfo.CatchType
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

		private sealed class EHInfo {
			public int TryStart;
			public int TryEnd;
			public int FilterStart;
			public int HandlerStart;
			public int HandlerEnd;
			public readonly ITypeDefOrRef? CatchType;
			public readonly BlockType HandlerType;

			public EHInfo(ExceptionHandler exceptionHandler, Dictionary<Instruction, int> instructionMap) {
				TryStart = instructionMap[exceptionHandler.TryStart];
				TryEnd = !(exceptionHandler.TryEnd is null) ? instructionMap[exceptionHandler.TryEnd] : instructionMap.Count;
				// try
				FilterStart = !(exceptionHandler.FilterStart is null) ? instructionMap[exceptionHandler.FilterStart] : -1;
				// filter
				HandlerStart = instructionMap[exceptionHandler.HandlerStart];
				HandlerEnd = !(exceptionHandler.HandlerEnd is null) ? instructionMap[exceptionHandler.HandlerEnd] : instructionMap.Count;
				// handler
				CatchType = exceptionHandler.CatchType;
				HandlerType = exceptionHandler.HandlerType switch
				{
					ExceptionHandlerType.Catch => BlockType.Catch,
					ExceptionHandlerType.Filter => BlockType.Filter,
					ExceptionHandlerType.Finally => BlockType.Finally,
					ExceptionHandlerType.Fault => BlockType.Fault,
					_ => throw new ArgumentOutOfRangeException(),
				};
			}

			public bool IsTryEqual(EHInfo other) {
				return other.TryStart == TryStart && other.TryEnd == TryEnd;
			}
		}

		private sealed class EHNode {
			public readonly List<EHInfo> Infos;
			public int Start;
			public int End;

			public EHNode(EHInfo value) {
				Infos = new List<EHInfo> { value };
			}
		}
	}
}
