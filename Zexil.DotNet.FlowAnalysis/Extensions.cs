using System;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Gets the first basic block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IBasicBlock First(this IBlock block) {
			return (IBasicBlock)Impl(block);

			static IBlock Impl(IBlock b) {
				if (b is IScopeBlock scopeBlock)
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
		public static IBasicBlock Last(this IBlock block) {
			return (IBasicBlock)Impl(block);

			static IBlock Impl(IBlock b) {
				if (b is IScopeBlock scopeBlock)
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
		public static IBlock Upward(this IBlock block, IScopeBlock scope) {
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
		public static IBlock? UpwardThrow(this IBlock block, IScopeBlock scope) {
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
		/// Enumerates blocks by type
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> Enumerate<TBlock>(this IBlock block) where TBlock : IBlock {
			return block.EnumerateInward<TBlock>();
		}

		/// <summary>
		/// Enumerates blocks by type
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> Enumerate<TBlock>(this IEnumerable<IBlock> blocks) where TBlock : IBlock {
			return blocks.EnumerateInward<TBlock>();
		}

		/// <summary>
		/// Enumerates blocks by type from outer blocks to inner blocks
		/// </summary>
		/// <typeparam name="TBlock"></typeparam>
		/// <param name="block"></param>
		/// <returns></returns>
		public static IEnumerable<TBlock> EnumerateInward<TBlock>(this IBlock block) where TBlock : IBlock {
			if (block is TBlock t1)
				yield return t1;
			if (block is IScopeBlock scopeBlock) {
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
		public static IEnumerable<TBlock> EnumerateInward<TBlock>(this IEnumerable<IBlock> blocks) where TBlock : IBlock {
			foreach (var block in blocks) {
				switch (block) {
				case IBasicBlock _:
					if (block is TBlock t1)
						yield return t1;
					break;
				case IScopeBlock scopeBlock:
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
		public static IEnumerable<TBlock> EnumerateOutward<TBlock>(this IBlock block) where TBlock : IBlock {
			if (block is IScopeBlock scopeBlock) {
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
		public static IEnumerable<TBlock> EnumerateOutward<TBlock>(this IEnumerable<IBlock> blocks) where TBlock : IBlock {
			foreach (var block in blocks) {
				switch (block) {
				case IBasicBlock _:
					if (block is TBlock t1)
						yield return t1;
					break;
				case IScopeBlock scopeBlock:
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
		/// Inserts the elements of a collection into the <see cref="IList{T}"/> at the specified index.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="index"></param>
		/// <param name="collection"></param>
		public static void InsertRange<T>(this IList<T> list, int index, IEnumerable<T> collection) {
			if (!(collection is ICollection<T> c))
				c = new List<T>(collection);
			if (list is List<T> list2) {
				list2.InsertRange(index, c);
			}
			else {
				int length = list.Count;
#pragma warning disable CS8604 // Possible null reference argument.
				for (int i = 0; i < c.Count; i++)
					list.Add(default);
#pragma warning restore CS8604 // Possible null reference argument.
				for (int i = index; i < length; i++)
					list[i + c.Count] = list[i];
				int n = 0;
				foreach (var item in c)
					list[index + n++] = item;
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
