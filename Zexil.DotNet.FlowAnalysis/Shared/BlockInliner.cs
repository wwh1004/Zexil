using System;
using System.Linq;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Shared {
	/// <summary>
	/// Block inliner common code
	/// </summary>
	internal static class BlockInliner {
		/// <summary>
		/// Inlines all basic blocks as much as possible.
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <param name=""></param>
		/// <param name="redirect"></param>
		/// <param name="concat"></param>
		/// <param name="erase"></param>
		/// <returns></returns>
		public static int Inline(IScopeBlock methodBlock, Func<IBasicBlock,>, Action<IBasicBlock, IBasicBlock> redirect, Action<IBasicBlock, IBasicBlock> concat, Action<IBasicBlock> erase) {
			int count = 0;
			foreach (var block in methodBlock.Enumerate<IBlock>()) {
				if (block is IBasicBlock basicBlock) {
					if ((basicBlock.Flags & BlockFlags.NoInlining) == BlockFlags.NoInlining)
						continue;

					if (basicBlock.IsEmpty && basicBlock.BranchOpcode.Code == Code.Br) {
						// If basic block is empty and branch opcode is br, we can redirect targets.
						basicBlock.Redirect(basicBlock.FallThrough);
						basicBlock.Erase();
						count++;
					}
					else {
						if (basicBlock.Predecessors.Count != 1 || basicBlock == basicBlock.Scope.First())
							continue;
						// Can't be inlined if has more than one predecessors or has no predecessor (not used basic block) or basic block is the first in current scope
						var predecessor = basicBlock.Predecessors.Keys.First();
						if (predecessor.FlowControl != FlowControl.Branch || predecessor.Scope != basicBlock.Scope || (predecessor.Flags & BlockFlags.NoInlining) == BlockFlags.NoInlining)
							continue;
						// Only br basic block and in the same scope then we can inline.
						predecessor.Concat(basicBlock);
						count++;
					}
				}
				else if (block is IScopeBlock scopeBlock) {
					// We should fix (just exchange) entry if first basic block is empty and let it be inlined later.
					if (!(scopeBlock.FirstBlock is IBasicBlock first) || !first.IsEmpty || first.BranchOpCode.Code != Code.Br)
						continue;

					var blocks = scopeBlock.Blocks;
					var fallThrough = first;
					do {
						fallThrough = fallThrough.FallThrough;
					} while (fallThrough.IsEmpty && fallThrough.BranchOpcode.Code == Code.Br && fallThrough.Scope == scopeBlock);
					// Gets final target basic block
					var fallThroughParent = fallThrough.Upward(scopeBlock);
					// Gets parent of which scope is scopeBlock

					int index = blocks.IndexOf(fallThroughParent);
					blocks[0] = fallThroughParent;
					blocks[index] = first;
					// Exchanges index
				}
			}
			return count;
		}
	}
}
