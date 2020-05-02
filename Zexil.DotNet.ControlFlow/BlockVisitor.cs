using System;
using System.Collections.Generic;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block visitor
	/// </summary>
	public abstract class BlockVisitor {
		/// <summary>
		/// Visits
		/// </summary>
		/// <param name="blocks">Blocks to visit</param>
		protected void Visit(IEnumerable<Block> blocks) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			foreach (var block in blocks)
				Visit(block);
		}

		/// <summary>
		/// Visits
		/// </summary>
		/// <param name="block">Block to visit</param>
		protected void Visit(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			if (block is BasicBlock) {
				OnBasicBlock((BasicBlock)block);
			}
			else if (block is TryBlock tryBlock) {
				OnScopeBlockEnter(tryBlock);
				OnTryBlockEnter(tryBlock);
				Visit(tryBlock.Blocks);
				OnTryBlockLeave(tryBlock);
				OnScopeBlockLeave(tryBlock);

				Visit(tryBlock.Handlers);
			}
			else if (block is FilterBlock filterBlock) {
				OnScopeBlockEnter(filterBlock);
				OnFilterBlockEnter(filterBlock);
				Visit(filterBlock.Blocks);
				OnFilterBlockLeave(filterBlock);
				OnScopeBlockLeave(filterBlock);
			}
			else if (block is HandlerBlock handlerBlock) {
				if (!(handlerBlock.Filter is null))
					Visit(handlerBlock.Filter);

				OnScopeBlockEnter(handlerBlock);
				OnHandlerBlockEnter(handlerBlock);
				Visit(handlerBlock.Blocks);
				OnHandlerBlockLeave(handlerBlock);
				OnScopeBlockLeave(handlerBlock);
			}
			else if (block is MethodBlock methodBlock) {
				OnScopeBlockEnter(methodBlock);
				OnMethodBlockEnter(methodBlock);
				Visit(methodBlock.Blocks);
				OnMethodBlockLeave(methodBlock);
				OnScopeBlockLeave(methodBlock);
			}
			else if (block is UserBlock userBlock) {
				OnScopeBlockEnter(userBlock);
				OnUserBlockEnter(userBlock);
				Visit(userBlock.Blocks);
				OnUserBlockLeave(userBlock);
				OnScopeBlockLeave(userBlock);
			}
			else {
				throw new InvalidOperationException();
			}
		}

		/// <summary>
		/// Called when a block is basic block
		/// </summary>
		/// <param name="basicBlock"></param>
		protected virtual void OnBasicBlock(BasicBlock basicBlock) {
		}

		/// <summary>
		/// Called before entering any scope block
		/// </summary>
		/// <param name="scopeBlock"></param>
		protected virtual void OnScopeBlockEnter(ScopeBlock scopeBlock) {
		}

		/// <summary>
		/// Called after entering any scope block
		/// </summary>
		/// <param name="scopeBlock"></param>
		protected virtual void OnScopeBlockLeave(ScopeBlock scopeBlock) {
		}

		/// <summary>
		/// Called before entering a try block
		/// </summary>
		/// <param name="tryBlock"></param>
		protected virtual void OnTryBlockEnter(TryBlock tryBlock) {
		}

		/// <summary>
		/// Called after entering a try block
		/// </summary>
		/// <param name="tryBlock"></param>
		protected virtual void OnTryBlockLeave(TryBlock tryBlock) {
		}

		/// <summary>
		/// Called before entering a filter block
		/// </summary>
		/// <param name="filterBlock"></param>
		protected virtual void OnFilterBlockEnter(FilterBlock filterBlock) {
		}

		/// <summary>
		/// Called after entering a filter block
		/// </summary>
		/// <param name="filterBlock"></param>
		protected virtual void OnFilterBlockLeave(FilterBlock filterBlock) {
		}

		/// <summary>
		/// Called before entering a handler block
		/// </summary>
		/// <param name="handlerBlock"></param>
		protected virtual void OnHandlerBlockEnter(HandlerBlock handlerBlock) {
		}

		/// <summary>
		/// Called after entering a handler block
		/// </summary>
		/// <param name="handlerBlock"></param>
		protected virtual void OnHandlerBlockLeave(HandlerBlock handlerBlock) {
		}

		/// <summary>
		/// Called before entering a method block
		/// </summary>
		/// <param name="methodBlock"></param>
		protected virtual void OnMethodBlockEnter(MethodBlock methodBlock) {
		}

		/// <summary>
		/// Called after entering a method block
		/// </summary>
		/// <param name="methodBlock"></param>
		protected virtual void OnMethodBlockLeave(MethodBlock methodBlock) {
		}

		/// <summary>
		/// Called before entering a user block
		/// </summary>
		/// <param name="userBlock"></param>
		protected virtual void OnUserBlockEnter(UserBlock userBlock) {
		}

		/// <summary>
		/// Called after entering a user block
		/// </summary>
		/// <param name="userBlock"></param>
		protected virtual void OnUserBlockLeave(UserBlock userBlock) {
		}
	}
}
