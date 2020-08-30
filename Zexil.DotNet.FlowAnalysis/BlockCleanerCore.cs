using System;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	internal static class BlockCleanerCore {
		public static int RemoveUnusedBlocks(IScopeBlock methodBlock, Action<IBasicBlock> eraser) {
			var isVisiteds = new HashSet<IBlock>();
			VisitSuccessors(methodBlock.First(), isVisiteds);
			int count = 0;
			foreach (var scopeBlock in methodBlock.Enumerate<IScopeBlock>()) {
				int c = 0;
				var blocks = scopeBlock.Blocks;
				for (int i = 0; i < blocks.Count; i++) {
					if (isVisiteds.Contains(blocks[i])) {
						blocks[i - c] = blocks[i];
					}
					else {
						if (blocks[i] is IBasicBlock basicBlock)
							eraser(basicBlock);
						c++;
					}
				}
				blocks.RemoveRange(blocks.Count - c, c);
				count += c;
			}
			return count;

			static void VisitSuccessors(IBasicBlock basicBlock, HashSet<IBlock> isVisiteds) {
				if (!isVisiteds.Add(basicBlock))
					return;
				VisitSuccessorsCore(basicBlock, isVisiteds);
			}

			static void VisitSuccessorsCore(IBasicBlock basicBlock, HashSet<IBlock> isVisiteds) {
				VisitScope(basicBlock, isVisiteds);
				foreach (var successor in basicBlock.Successors) {
					if (!isVisiteds.Add(successor.Key))
						continue;
					VisitSuccessorsCore(successor.Key, isVisiteds);
				}
			}

			static void VisitScope(IBlock block, HashSet<IBlock> isVisiteds) {
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
