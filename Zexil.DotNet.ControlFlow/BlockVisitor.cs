using System;
using System.Collections.Generic;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block visitor
	/// </summary>
	public static class BlockVisitor {
		/// <summary>
		/// Visit all blocks
		/// </summary>
		/// <param name="blocks"></param>
		/// <param name="onBlockEnter">Called when enters a block</param>
		/// <param name="onBlockLeave">Called when leaves a block</param>
		/// <returns></returns>
		public static bool Visit(IEnumerable<Block> blocks, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			bool isModified = false;
			VisitCore(blocks, ref isModified, onBlockEnter, onBlockLeave);
			return isModified;
		}

		/// <summary>
		/// Visit all blocks
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onBlockEnter">Called when enters a block</param>
		/// <param name="onBlockLeave">Called when leaves a block</param>
		/// <returns></returns>
		public static bool Visit(Block block, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			bool isModified = false;
			VisitCore(block, ref isModified, onBlockEnter, onBlockLeave);
			return isModified;
		}

		private static void VisitCore(IEnumerable<Block> blocks, ref bool isModified, BlockHandler? onBlockEnter, BlockHandler? onBlockLeave) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			foreach (var block in blocks)
				VisitCore(block, ref isModified, onBlockEnter, onBlockLeave);
		}

		private static void VisitCore(Block block, ref bool isModified, BlockHandler? onBlockEnter, BlockHandler? onBlockLeave) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			if (block is BasicBlock) {
				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);
			}
			else if (block is TryBlock tryBlock) {
				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				VisitCore(tryBlock.Blocks, ref isModified, onBlockEnter, onBlockLeave);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);

				VisitCore(tryBlock.Handlers, ref isModified, onBlockEnter, onBlockLeave);
			}
			else if (block is FilterBlock filterBlock) {
				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				VisitCore(filterBlock.Blocks, ref isModified, onBlockEnter, onBlockLeave);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);
			}
			else if (block is HandlerBlock handlerBlock) {
				if (!(handlerBlock.Filter is null))
					VisitCore(handlerBlock.Filter, ref isModified, onBlockEnter, onBlockLeave);

				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				VisitCore(handlerBlock.Blocks, ref isModified, onBlockEnter, onBlockLeave);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);
			}
			else if (block is MethodBlock methodBlock) {
				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				VisitCore(methodBlock.Blocks, ref isModified, onBlockEnter, onBlockLeave);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);
			}
			else if (block is UserBlock userBlock) {
				if (!(onBlockEnter is null))
					isModified |= onBlockEnter(block);
				VisitCore(userBlock.Blocks, ref isModified, onBlockEnter, onBlockLeave);
				if (!(onBlockLeave is null))
					isModified |= onBlockLeave(block);
			}
			else {
				throw new InvalidOperationException();
			}
		}
	}
}
