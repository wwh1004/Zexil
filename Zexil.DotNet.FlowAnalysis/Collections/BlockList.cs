using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis.Collections {
	internal sealed class BlockList<TBlock> : List<TBlock>, IList<IBlock> where TBlock : IBlock {
		public BlockList() {
		}

		public BlockList(IEnumerable<TBlock> collection) : base(collection) {
		}

		public BlockList(int capacity) : base(capacity) {
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
			if (array is TBlock[] array2) {
				CopyTo(array2, arrayIndex);
			}
			else {
				array2 = new TBlock[Count];
				CopyTo(array2, 0);
				for (int i = 0; i < array2.Length; i++)
					array[arrayIndex + i] = array2[i];
			}
		}

		IEnumerator<IBlock> IEnumerable<IBlock>.GetEnumerator() {
			return (IEnumerator<IBlock>)(IEnumerator<TBlock>)GetEnumerator();
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
	}
}
