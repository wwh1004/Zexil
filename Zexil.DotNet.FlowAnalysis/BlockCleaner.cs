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
			BlockVisitor.VisitAll(block, onBlockEnter: b => {
				if (!(b is BasicBlock basicBlock))
					return false;

				int c = 0;
				var instructions = basicBlock.Instructions;
				for (int i = 0; i < instructions.Count; i++) {
					if (instructions[i].OpCode.Code == Code.Nop)
						c++;
					else
						instructions[i - c] = instructions[i];
				}
				count += c;
				return false;
			});
			return count;
		}

		/// <summary>
		/// Removes all unused blocks
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <returns></returns>
		public static int RemoveUnusedBlocks(MethodBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));

			var isVisiteds = new HashSet<Block>();
			VisitSuccessors(methodBlock.First(), isVisiteds);
			int count = 0;
			BlockVisitor.VisitAll(methodBlock, onBlockEnter: b => {
				if (!(b is ScopeBlock scopeBlock))
					return false;

				int c = 0;
				var blocks = scopeBlock.Blocks;
				for (int i = 0; i < blocks.Count; i++) {
					if (isVisiteds.Contains(blocks[i]))
						blocks[i - c] = blocks[i];
					else
						c++;
				}
				((List<Block>)blocks).RemoveRange(blocks.Count - c, c);
				count += c;
				return c != 0;
			});
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
					if (block is MethodBlock)
						break;
					var scope = block.Scope;
					if (!isVisiteds.Add(scope))
						break;
					if (scope is TryBlock tryBlock) {
						foreach (var handler in tryBlock.Handlers) {
							if (!(handler.Filter is null))
								VisitSuccessors(handler.Filter.First(), isVisiteds);
							VisitSuccessors(handler.First(), isVisiteds);
						}
					}
					block = scope;
				}
			}
		}
	}
}
