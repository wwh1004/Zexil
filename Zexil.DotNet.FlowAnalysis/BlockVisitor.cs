using System;
using System.Collections.Generic;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block visitor
	/// </summary>
	public sealed class BlockVisitor {
		private readonly BlockHandler? _onBlockEnter;
		private readonly BlockHandler? _onBlockLeave;
		private bool _isModified;

		private BlockVisitor(BlockHandler? onBlockEnter, BlockHandler? onBlockLeave) {
			_onBlockEnter = onBlockEnter;
			_onBlockLeave = onBlockLeave;
		}

		/// <summary>
		/// Visit all blocks
		/// </summary>
		/// <param name="blocks"></param>
		/// <param name="onBlockEnter">Called when enters a block</param>
		/// <param name="onBlockLeave">Called when leaves a block</param>
		/// <returns></returns>
		public static bool VisitAll(IEnumerable<Block> blocks, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			var visitor = new BlockVisitor(onBlockEnter, onBlockLeave);
			visitor.VisitAllCore(blocks);
			return visitor._isModified;
		}

		/// <summary>
		/// Visit all blocks
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onBlockEnter">Called when enters a block</param>
		/// <param name="onBlockLeave">Called when leaves a block</param>
		/// <returns></returns>
		public static bool VisitAll(Block block, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			var visitor = new BlockVisitor(onBlockEnter, onBlockLeave);
			visitor.VisitAllCore(block);
			return visitor._isModified;
		}

		private void VisitAllCore(IEnumerable<Block> blocks) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			foreach (var block in blocks)
				VisitAllCore(block);
		}

		private void VisitAllCore(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			if (block is BasicBlock) {
				OnBlockEnter(block);
				OnBlockLeave(block);
			}
			else if (block is TryBlock tryBlock) {
				OnBlockEnter(block);
				VisitAllCore(tryBlock.Blocks);
				OnBlockLeave(block);

				VisitAllCore(tryBlock.Handlers);
			}
			else if (block is FilterBlock filterBlock) {
				OnBlockEnter(block);
				VisitAllCore(filterBlock.Blocks);
				OnBlockLeave(block);
			}
			else if (block is HandlerBlock handlerBlock) {
				if (!(handlerBlock.Filter is null))
					VisitAllCore(handlerBlock.Filter);

				OnBlockEnter(block);
				VisitAllCore(handlerBlock.Blocks);
				OnBlockLeave(block);
			}
			else if (block is MethodBlock methodBlock) {
				OnBlockEnter(block);
				VisitAllCore(methodBlock.Blocks);
				OnBlockLeave(block);
			}
			else if (block is UserBlock userBlock) {
				OnBlockEnter(block);
				VisitAllCore(userBlock.Blocks);
				OnBlockLeave(block);
			}
			else {
				throw new InvalidOperationException();
			}
		}

		private void OnBlockEnter(Block block) {
			if (!(_onBlockEnter is null))
				_isModified |= _onBlockEnter(block);
		}

		private void OnBlockLeave(Block block) {
			if (!(_onBlockLeave is null))
				_isModified |= _onBlockLeave(block);
		}
	}
}
