using System;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block sorter
	/// </summary>
	public static class BlockSorter {
		/// <summary>
		/// Sorts all blocks in <paramref name="methodBlock"/>
		/// NOTICE: Do NOT use it to remove unsed blocks! Please call <see cref="BlockCleaner.RemoveUnusedBlocks(MethodBlock)"/> first!
		/// </summary>
		/// <param name="methodBlock"></param>
		public static void Sort(MethodBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));

			object contextKey = new object();
			var emptyContext = new BlockContext();
			// If basic block has no any successors, we set empty context to reduce memory usage.
			BlockVisitor.VisitAll(methodBlock, onBlockEnter: b => {
				if (!(b is BasicBlock basicBlock))
					return false;
				if(basicBlock.Successors.Count == 0) {
					basicBlock.Contexts.Set(contextKey, emptyContext);
					return false;
				}

				var block = (Block)basicBlock;
				// block is jump source (jump from)
				bool flag = false;
				/*
				 * There are three situations and they appear in order
				 * 1. Jump out of scope
				 * 2. Add target(s) successfully (only appear once)
				 * 3. Self-loop
				 * When we add target(s) successfully, we should break do-while loop.
				 */
				do {
					if (!block.Contexts.TryGet<BlockContext>(contextKey, out var context))
						context = block.Contexts.Set(contextKey, new BlockContext());
					var targets = context.Targets;
					var scope = block.Scope;

					flag |= AddTarget(targets, scope, basicBlock.FallThroughNoThrow);
					flag |= AddTarget(targets, scope, basicBlock.CondTargetNoThrow);
					var switchTargets = basicBlock.SwitchTargetsNoThrow;
					if (!(switchTargets is null)) {
						foreach (var switchTarget in switchTargets)
							flag |= AddTarget(targets, scope, switchTarget);
					}
					// target is jump destination (jump to)

					block = block.Scope;
				} while (!flag && !(block is MethodBlock));
				return false;

				static bool AddTarget(List<Block> targets, ScopeBlock scope, BasicBlock? target) {
					if (target is null)
						return false;
					var parent = target.GetParentNoThrow(scope);
					if (parent is null)
						return false;
					// If basic block jump out of current scope, root will be null. We should skip it.
					if (!targets.Contains(parent))
						targets.Add(parent);
					return true;
				}
			});

			BlockVisitor.VisitAll(methodBlock, onBlockEnter: b => {
				if (!(b is ScopeBlock scopeBlock))
					return false;

				var blocks = scopeBlock.Blocks;
				var stack = new Stack<Block>();
				stack.Push(blocks[0]);
				int index = 0;
				do {
					var block = stack.Pop();
					if (!block.Contexts.TryRemove<BlockContext>(contextKey, out var context))
						continue;

					blocks[index++] = block;
					int targetCount = context.Targets.Count;
					for (int i = targetCount - 1; i >= 0; i--)
						stack.Push(context.Targets[i]);
				} while (index != blocks.Count);
				return false;
			});
		}

		private sealed class BlockContext : IBlockContext {
			public List<Block> Targets = new List<Block>();
		}
	}
}
