using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Creates method block from methodDef.
		/// NOTICE: <see cref="CilBody.SimplifyMacros"/> will be called!
		/// </summary>
		/// <param name="methodDef"></param>
		/// <returns></returns>
		public static ScopeBlock ToMethodBlock(this MethodDef methodDef) {
			methodDef.Body.SimplifyMacros(methodDef.Parameters);
			return CodeParser.Parse(methodDef.Body.Instructions, methodDef.Body.ExceptionHandlers);
		}

		/// <summary>
		/// Restores <see cref="MethodDef"/> from method block
		/// </summary>
		/// <param name="methodDef"></param>
		/// <param name="methodBlock"></param>
		public static void FromMethodBlock(this MethodDef methodDef, ScopeBlock methodBlock) {
			var body = methodDef.Body;
			CodeGenerator.Generate(methodBlock, out var instructions, out var exceptionHandlers, out var locals);
			body.Instructions.Clear();
			body.Instructions.AddRange(instructions);
			body.ExceptionHandlers.Clear();
			body.ExceptionHandlers.AddRange(exceptionHandlers);
			body.Variables.Clear();
			body.Variables.AddRange(locals);
		}

		/// <summary>
		/// Gets the first basic block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static BasicBlock First(this Block block) {
			return (BasicBlock)Impl(block);

			static Block Impl(Block b) {
				if (b is ScopeBlock scopeBlock)
					return Impl(scopeBlock.FirstBlock);
				else
					return b;
			}
		}

		/// <summary>
		/// Gets the last basic block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static BasicBlock Last(this Block block) {
			return (BasicBlock)Impl(block);

			static Block Impl(Block b) {
				if (b is ScopeBlock scopeBlock)
					return Impl(scopeBlock.LastBlock);
				else
					return b;
			}
		}

		/// <summary>
		/// Gets block's parent or self of which scope is parameter <paramref name="scope"/> and throws if null
		/// </summary>
		/// <param name="block"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static Block Upward(this Block block, ScopeBlock scope) {
			var root = block;
			while (root.Scope != scope)
				root = root.Scope;
			return root;
		}

		/// <summary>
		/// Gets block's parent or self of which scope is parameter <paramref name="scope"/>
		/// </summary>
		/// <param name="block"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static Block? UpwardThrow(this Block block, ScopeBlock scope) {
			var root = block;
			while (root.ScopeNoThrow != scope) {
				if (root.Type == BlockType.Method)
					return null;
				else
					root = root.Scope;
			}
			return root;
		}

		/// <summary>
		/// Redirect branches from basicBlock to newTarget
		/// </summary>
		/// <param name="basicBlock"></param>
		/// <param name="newTarget"></param>
		public static void Redirect(this BasicBlock basicBlock, BasicBlock newTarget) {
			var predecessors = basicBlock.Predecessors.Keys.ToArray();
			foreach (var predecessor in predecessors) {
				if (predecessor.FallThroughNoThrow == basicBlock)
					predecessor.FallThroughNoThrow = newTarget;
				if (predecessor.CondTargetNoThrow == basicBlock)
					predecessor.CondTargetNoThrow = newTarget;
				var switchTargets = predecessor.SwitchTargetsNoThrow;
				if (!(switchTargets is null)) {
					for (int i = 0; i < switchTargets.Count; i++) {
						if (switchTargets[i] == basicBlock)
							switchTargets[i] = newTarget;
					}
				}
			}
		}

		/// <summary>
		/// Concatenates two basic blocks
		/// </summary>
		/// <param name="first"></param>
		/// <param name="second"></param>
		public static void Concat(this BasicBlock first, BasicBlock second) {
			first.Instructions.AddRange(second.Instructions);
			second.Instructions.Clear();
			first.BranchOpcode = second.BranchOpcode;
			second.BranchOpcode = OpCodes.Ret;
			first.FallThroughNoThrow = second.FallThroughNoThrow;
			second.FallThroughNoThrow = null;
			first.CondTargetNoThrow = second.CondTargetNoThrow;
			second.CondTargetNoThrow = null;
			var switchTargets = second.SwitchTargetsNoThrow;
			second.SwitchTargetsNoThrow = null;
			first.SwitchTargetsNoThrow = switchTargets;
		}

		/// <summary>
		/// Enumerates blocks by type
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> Enumerate<TBlock>(this Block block) where TBlock : Block {
			return block.EnumerateInward<TBlock>();
		}

		/// <summary>
		/// Enumerates blocks by type
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> Enumerate<TBlock>(this IEnumerable<Block> blocks) where TBlock : Block {
			return blocks.EnumerateInward<TBlock>();
		}

		/// <summary>
		/// Enumerates blocks by type from outer blocks to inner blocks
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> EnumerateInward<TBlock>(this Block block) where TBlock : Block {
			if (block is TBlock t1)
				yield return t1;
			if (block is ScopeBlock scopeBlock) {
				foreach (var t2 in scopeBlock.Blocks.EnumerateInward<TBlock>())
					yield return t2;
			}
		}

		/// <summary>
		/// Enumerates blocks by type from outer blocks to inner blocks
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> EnumerateInward<TBlock>(this IEnumerable<Block> blocks) where TBlock : Block {
			foreach (var block in blocks) {
				switch (block) {
				case BasicBlock _:
					if (block is TBlock t1)
						yield return t1;
					break;
				case ScopeBlock scopeBlock:
					if (block is TBlock t2)
						yield return t2;
					foreach (var t3 in scopeBlock.Blocks.EnumerateInward<TBlock>())
						yield return t3;
					break;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Enumerates blocks by type from inner blocks to outer blocks
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> EnumerateOutward<TBlock>(this Block block) where TBlock : Block {
			if (block is ScopeBlock scopeBlock) {
				foreach (var t1 in scopeBlock.Blocks.EnumerateOutward<TBlock>())
					yield return t1;
			}
			if (block is TBlock t2)
				yield return t2;
		}

		/// <summary>
		/// Enumerates blocks by type from inner blocks to outer blocks
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> EnumerateOutward<TBlock>(this IEnumerable<Block> blocks) where TBlock : Block {
			foreach (var block in blocks) {
				switch (block) {
				case BasicBlock _:
					if (block is TBlock t1)
						yield return t1;
					break;
				case ScopeBlock scopeBlock:
					foreach (var t2 in scopeBlock.Blocks.EnumerateOutward<TBlock>())
						yield return t2;
					if (block is TBlock t3)
						yield return t3;
					break;
				default:
					throw new InvalidOperationException();
				}
			}
		}

		/// <summary>
		/// Adds items to a list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="collection"></param>
		public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection) {
			if (list is List<T> list2) {
				list2.AddRange(collection);
			}
			else {
				foreach (var item in collection)
					list.Add(item);
			}
		}

		/// <summary>
		/// Removes a range of elements from the <see cref="IList{T}"/>
		/// </summary>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <param name="count"></param>
		public static void RemoveRange<T>(this IList<T> list, int index, int count) {
			if (list is List<T> list2) {
				list2.RemoveRange(index, count);
			}
			else {
				for (int i = index + count - 1; i >= index; i--)
					list.RemoveAt(i);
				// forr has better performance than for
			}
		}
	}
}
