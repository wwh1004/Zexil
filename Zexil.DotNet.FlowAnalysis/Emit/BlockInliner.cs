using System;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	/// <summary>
	/// Block inliner
	/// </summary>
	public static class BlockInliner {
		/// <summary>
		/// Inlines all basic blocks as much as possible.
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <returns></returns>
		public static int Inline(ScopeBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));
			if (methodBlock.Type != BlockType.Method)
				throw new ArgumentException($"{nameof(methodBlock)} is not a method block");

			return Shared.BlockInliner.Inline(methodBlock, Redirect, Concat, Erase);

			static void Redirect(IBasicBlock basicBlock, IBasicBlock newTarget) {
				((BasicBlock)basicBlock).Redirect((BasicBlock)newTarget);
			}

			static void Concat(IBasicBlock first, IBasicBlock second) {
				((BasicBlock)first).Concat((BasicBlock)second);
			}

			static void Erase(IBasicBlock basicBlock) {
				((BasicBlock)basicBlock).Erase();
			}
		}
	}
}
