using System;
using System.Linq;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block inliner
	/// </summary>
	public static class BlockInliner {
		/// <summary>
		/// Inlines all basic blocks as much as possible.
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <returns></returns>
		public static int Inline(MethodBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));

			int count = 0;
			BlockVisitor.VisitAll(methodBlock, onBlockEnter: b => {
				if (b is BasicBlock basicBlock) {
					if (basicBlock.IsEmpty && basicBlock.BranchOpcode.Code == Code.Br) {
						// If basic block is empty and branch opcode is br, we can redirect targets.
						var fallThrough = basicBlock.FallThrough;
						var predecessors = basicBlock.Predecessors.Keys.ToArray();
						foreach (var predecessor in predecessors) {
							if (predecessor.FallThroughNoThrow == basicBlock)
								predecessor.FallThroughNoThrow = fallThrough;
							if (predecessor.CondTargetNoThrow == basicBlock)
								predecessor.CondTargetNoThrow = fallThrough;
							var switchTargets = predecessor.SwitchTargetsNoThrow;
							if (!(switchTargets is null)) {
								for (int i = 0; i < switchTargets.Count; i++) {
									if (switchTargets[i] == basicBlock)
										switchTargets[i] = fallThrough;
								}
							}
						}
						count++;
						return true;
					}
					else {
						if ((basicBlock.Flags & BlockFlags.NoInlining) == BlockFlags.NoInlining || basicBlock.Predecessors.Count != 1 || basicBlock == basicBlock.Scope.First())
							return false;
						// Can't be inlined if has more than one predecessors or has no predecessor (not used basic block) or basic block is the first in current scope
						var predecessor = basicBlock.Predecessors.Keys.First();
						if (predecessor.BranchOpcode.Code != Code.Br || predecessor.Scope != basicBlock.Scope)
							return false;
						// Only br basic block and in the same scope then we can inline.

						predecessor.Instructions.AddRange(basicBlock.Instructions);
						basicBlock.Instructions.Clear();
						predecessor.BranchOpcode = basicBlock.BranchOpcode;
						basicBlock.BranchOpcode = OpCodes.Ret;
						predecessor.FallThroughNoThrow = basicBlock.FallThroughNoThrow;
						basicBlock.FallThroughNoThrow = null;
						predecessor.CondTargetNoThrow = basicBlock.CondTargetNoThrow;
						basicBlock.CondTargetNoThrow = null;
						var switchTargets = basicBlock.SwitchTargetsNoThrow;
						basicBlock.SwitchTargetsNoThrow = null;
						predecessor.SwitchTargetsNoThrow = switchTargets;
						count++;
						return true;
					}
				}
				else if (b is ScopeBlock scopeBlock) {
					// We should fix entry if first basic block is empty.
					if (!(scopeBlock.FirstBlock is BasicBlock first) || !first.IsEmpty || first.BranchOpcode.Code != Code.Br)
						return false;

					var blocks = scopeBlock.Blocks;
					var fallThrough = first;
					do {
						fallThrough = fallThrough.FallThrough;
					} while (fallThrough.IsEmpty && fallThrough.BranchOpcode.Code == Code.Br && fallThrough.Scope == scopeBlock);
					// Gets final target basic block
					var fallThroughRoot = fallThrough.GetRoot(scopeBlock);
					// Gets root of which scope is scopeBlock

					int index = blocks.IndexOf(fallThroughRoot);
					blocks[0] = fallThroughRoot;
					blocks[index] = first;
					// Exchanges index
					return true;
				}
				else {
					return false;
				}
			});
			return count;
		}
	}
}
