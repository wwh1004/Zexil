using System;
using System.Collections;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis.Collections {
	internal sealed class BlockList<TBlock> : List<TBlock>, IList<IBlock> where TBlock : IBlock {
		public BlockList() {
		}

		public BlockList(IEnumerable<TBlock> collection) : base(collection) {
		}

		IBlock IList<IBlock>.this[int index] {
			get => this[index];
			set => this[index] = (TBlock)value;
		}

		bool ICollection<IBlock>.IsReadOnly => false;

		void ICollection<IBlock>.Add(IBlock item) {
			Add((TBlock)item);
		}

		bool ICollection<IBlock>.Contains(IBlock item) {
			return Contains((TBlock)item);
		}

		void ICollection<IBlock>.CopyTo(IBlock[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		IEnumerator<IBlock> IEnumerable<IBlock>.GetEnumerator() {
			return new Enumerator2(GetEnumerator());
		}

		int IList<IBlock>.IndexOf(IBlock item) {
			return IndexOf((TBlock)item);
		}

		void IList<IBlock>.Insert(int index, IBlock item) {
			Insert(index, (TBlock)item);
		}

		bool ICollection<IBlock>.Remove(IBlock item) {
			return Remove((TBlock)item);
		}

		private struct Enumerator2 : IEnumerator<IBlock> {
			private readonly IEnumerator<TBlock> _enumerator;

			public Enumerator2(IEnumerator<TBlock> enumerator) {
				_enumerator = enumerator;
			}

			public IBlock Current => _enumerator.Current;

			object IEnumerator.Current => ((IEnumerator)_enumerator).Current;

			public void Dispose() {
				_enumerator.Dispose();
			}

			public bool MoveNext() {
				return _enumerator.MoveNext();
			}

			public void Reset() {
				_enumerator.Reset();
			}
		}
	}
}
