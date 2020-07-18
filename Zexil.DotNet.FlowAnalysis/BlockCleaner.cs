using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block cleaner
	/// </summary>
	public static class BlockCleaner {
		/// <summary>
		/// Removes all nops
		/// </summary>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static int RemoveNops(IEnumerable<Block> blocks) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			int count = 0;
			foreach (var block in blocks)
				count += RemoveNops(block);
			return count;
		}

		/// <summary>
		/// Removes all nops
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static int RemoveNops(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			int count = 0;
			foreach (var basicBlock in block.Enumerate<BasicBlock>()) {
				int c = 0;
				var instructions = basicBlock.Instructions;
				for (int i = 0; i < instructions.Count; i++) {
					if (instructions[i].OpCode.Code == Code.Nop)
						c++;
					else
						instructions[i - c] = instructions[i];
				}
				instructions.RemoveRange(instructions.Count - c, c);
				count += c;
			}
			return count;
		}

		/// <summary>
		/// Removes all unused blocks
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <returns></returns>
		public static int RemoveUnusedBlocks(ScopeBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));
			if (methodBlock.Type != BlockType.Method)
				throw new ArgumentException($"{nameof(methodBlock)} is not a method block");

			var isVisiteds = new HashSet<Block>();
			VisitSuccessors(methodBlock.First(), isVisiteds);
			int count = 0;
			foreach (var scopeBlock in methodBlock.Enumerate<ScopeBlock>()) {
				int c = 0;
				var blocks = scopeBlock.Blocks;
				for (int i = 0; i < blocks.Count; i++) {
					if (isVisiteds.Contains(blocks[i]))
						blocks[i - c] = blocks[i];
					else
						c++;
				}
				blocks.RemoveRange(blocks.Count - c, c);
				count += c;
			}
			return count;

			static void VisitSuccessors(BasicBlock basicBlock, HashSet<Block> isVisiteds) {
				if (!isVisiteds.Add(basicBlock))
					return;
				VisitSuccessorsCore(basicBlock, isVisiteds);
			}

			static void VisitSuccessorsCore(BasicBlock basicBlock, HashSet<Block> isVisiteds) {
				VisitScope(basicBlock, isVisiteds);
				foreach (var successor in basicBlock.Successors) {
					if (!isVisiteds.Add(successor.Key))
						continue;
					VisitSuccessorsCore(successor.Key, isVisiteds);
				}
			}

			static void VisitScope(Block block, HashSet<Block> isVisiteds) {
				while (true) {
					if (block.Type == BlockType.Method)
						break;
					var scope = block.Scope;
					if (!isVisiteds.Add(scope))
						break;
					if (scope.Type == BlockType.Protected) {
						foreach (var ehBlock in scope.Blocks) {
							if (ehBlock.Type == BlockType.Try)
								continue;
							VisitSuccessors(ehBlock.First(), isVisiteds);
						}
					}
					block = scope;
				}
			}
		}
	}
}
