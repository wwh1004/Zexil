using System;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	// TODO: refacoring, do NOT sort by dfs

	/// <summary>
	/// Block sorter
	/// </summary>
	public static class BlockSorter {
		/// <summary>
		/// Sorts all blocks in <paramref name="methodBlock"/>
		/// NOTICE: Do NOT use it to remove unsed blocks! Please call <see cref="BlockCleaner.RemoveUnusedBlocks(ScopeBlock)"/> first!
		/// </summary>
		/// <param name="methodBlock"></param>
		public static void Sort(ScopeBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));
			if (methodBlock.Type != BlockType.Method)
				throw new ArgumentException($"{nameof(methodBlock)} is not a method block");

			object contextKey = new object();
			foreach (var basicBlock in methodBlock.Enumerate<BasicBlock>()) {
				if (basicBlock.Successors.Count == 0) {
					basicBlock.Contexts.Set(contextKey, BlockContext.Empty);
					continue;
				}

				var block = (Block)basicBlock;
				// block is jump source (jump from)
				bool added;
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

					added = AddTarget(targets, scope, basicBlock.FallThroughNoThrow);
					// target is jump destination (jump to)
					if (added) {
						// If fall through is added successfully, then we can try adding other targets
						AddTarget(targets, scope, basicBlock.CondTargetNoThrow);
						var switchTargets = basicBlock.SwitchTargetsNoThrow;
						if (!(switchTargets is null)) {
							foreach (var switchTarget in switchTargets)
								AddTarget(targets, scope, switchTarget);
						}
					}
					else {
						block = block.Scope.Scope;
						// Gets upper protected block as jump source, not its child blocks (try, catch, ...)
					}
				} while (!added && block.Type != BlockType.Method);

				static bool AddTarget(List<Block> targets, ScopeBlock scope, BasicBlock? target) {
					if (target is null)
						return false;
					var parent = target.UpwardThrow(scope);
					if (parent is null)
						return false;
					// If basic block jump out of current scope, parent will be null. We should skip it.
					if (!targets.Contains(parent))
						targets.Add(parent);
					return true;
				}
			}

			foreach (var scopeBlock in methodBlock.Enumerate<ScopeBlock>()) {
				if (scopeBlock.Type == BlockType.Protected)
					continue;

				var blocks = scopeBlock.Blocks;
				var stack = new Stack<Block>();
				stack.Push(blocks[0]);
				int index = 0;
				do {
					var block = stack.Pop();
					if (!block.Contexts.TryRemove<BlockContext>(contextKey, out var context))
						continue;

					if (block.Scope != scopeBlock)
						throw new InvalidOperationException();
					blocks[index++] = block;
					int targetCount = context.Targets.Count;
					for (int i = targetCount - 1; i >= 0; i--)
						stack.Push(context.Targets[i]);
				} while (stack.Count > 0);
				if (index != blocks.Count)
					throw new InvalidOperationException($"Contains not used blocks, please call {nameof(BlockCleaner.RemoveUnusedBlocks)} first.");
			}
		}

		private sealed class BlockContext : IBlockContext {
			public static readonly BlockContext Empty = new BlockContext();
			// If basic block has no any successors, we set empty context to reduce memory usage.
			public List<Block> Targets = new List<Block>();
		}
	}
}
