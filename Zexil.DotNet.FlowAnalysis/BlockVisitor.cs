using System;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block visitor (more customizable than <see cref="Extensions.Enumerate{TBlock}(Block)"/> and <see cref="Extensions.Enumerate{TBlock}(IEnumerable{Block})"/>)
	/// </summary>
	public sealed class BlockVisitor {
		private readonly BlockHandler? _onBlockEnter;
		private readonly BlockHandler? _onBlockLeave;
		private int _count;

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
		public static int Visit(IEnumerable<Block> blocks, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			var visitor = new BlockVisitor(onBlockEnter, onBlockLeave);
			visitor.VisitCore(blocks);
			return visitor._count;
		}

		/// <summary>
		/// Visit all blocks
		/// </summary>
		/// <param name="block"></param>
		/// <param name="onBlockEnter">Called when enters a block</param>
		/// <param name="onBlockLeave">Called when leaves a block</param>
		/// <returns></returns>
		public static int Visit(Block block, BlockHandler? onBlockEnter = null, BlockHandler? onBlockLeave = null) {
			var visitor = new BlockVisitor(onBlockEnter, onBlockLeave);
			visitor.VisitCore(block);
			return visitor._count;
		}

		private void VisitCore(IEnumerable<Block> blocks) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			foreach (var block in blocks)
				VisitCore(block);
		}

		private void VisitCore(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			if (block is BasicBlock) {
				OnBlockEnter(block);
				OnBlockLeave(block);
			}
			else if (block is TryBlock tryBlock) {
				OnBlockEnter(block);
				VisitCore(tryBlock.Blocks);
				OnBlockLeave(block);

				VisitCore(tryBlock.Handlers);
			}
			else if (block is FilterBlock filterBlock) {
				OnBlockEnter(block);
				VisitCore(filterBlock.Blocks);
				OnBlockLeave(block);
			}
			else if (block is HandlerBlock handlerBlock) {
				if (!(handlerBlock.Filter is null))
					VisitCore(handlerBlock.Filter);

				OnBlockEnter(block);
				VisitCore(handlerBlock.Blocks);
				OnBlockLeave(block);
			}
			else if (block is MethodBlock methodBlock) {
				OnBlockEnter(block);
				VisitCore(methodBlock.Blocks);
				OnBlockLeave(block);
			}
			else {
				throw new InvalidOperationException();
			}
		}

		private void OnBlockEnter(Block block) {
			if (!(_onBlockEnter is null))
				_count += _onBlockEnter(block);
		}

		private void OnBlockLeave(Block block) {
			if (!(_onBlockLeave is null))
				_count += _onBlockLeave(block);
		}
	}
}
