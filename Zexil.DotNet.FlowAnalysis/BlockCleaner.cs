using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
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
			VisitAllSuccessors(methodBlock.First());
			BlockVisitor.VisitAll(methodBlock, onBlockEnter: b => {
				if (!(b is TryBlock tryBlock) || !isVisiteds.Contains(tryBlock))
					return false;

				foreach (var handlerBlock in tryBlock.Handlers) {
					if (!(handlerBlock.Filter is null))
						VisitAllSuccessors(handlerBlock.Filter.First());
					VisitAllSuccessors(handlerBlock.First());
				}
				return false;
			});

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

			void VisitAllSuccessors(BasicBlock basicBlock) {
				if (!isVisiteds.Add(basicBlock))
					return;
				VisitAllSuccessorsCore(basicBlock);
			}

			void VisitAllSuccessorsCore(BasicBlock basicBlock) {
				EnsureScopeVisited(basicBlock);
				foreach (var successor in basicBlock.Successors) {
					if (!isVisiteds.Add(successor.Key))
						continue;
					VisitAllSuccessorsCore(successor.Key);
				}
			}

			void EnsureScopeVisited(Block block) {
				if (block is MethodBlock)
					return;
				var scope = block.Scope;
				if (!isVisiteds.Add(scope))
					return;
				EnsureScopeVisited(scope);
			}
		}
	}
}
