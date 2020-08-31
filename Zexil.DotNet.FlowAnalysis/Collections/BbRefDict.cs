using System.Collections;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis.Collections {
	internal sealed class BbRefDict<TBasicBlock> : Dictionary<TBasicBlock, int>, IDictionary<IBasicBlock, int> where TBasicBlock : IBasicBlock {
		private KeyCollection2? _keys2;

		int IDictionary<IBasicBlock, int>.this[IBasicBlock key] {
			get => this[(TBasicBlock)key];
			set => this[(TBasicBlock)key] = value;
		}

		ICollection<IBasicBlock> IDictionary<IBasicBlock, int>.Keys {
			get {
				if (_keys2 is null)
					_keys2 = new KeyCollection2(this);
				return _keys2;
			}
		}

		ICollection<int> IDictionary<IBasicBlock, int>.Values => Values;

		bool ICollection<KeyValuePair<IBasicBlock, int>>.IsReadOnly => false;

		void IDictionary<IBasicBlock, int>.Add(IBasicBlock key, int value) {
			Add((TBasicBlock)key, value);
		}

		void ICollection<KeyValuePair<IBasicBlock, int>>.Add(KeyValuePair<IBasicBlock, int> item) {
			var item2 = new KeyValuePair<TBasicBlock, int>((TBasicBlock)item.Key, item.Value);
			((ICollection<KeyValuePair<TBasicBlock, int>>)this).Add(item2);
		}

		bool ICollection<KeyValuePair<IBasicBlock, int>>.Contains(KeyValuePair<IBasicBlock, int> item) {
			var item2 = new KeyValuePair<TBasicBlock, int>((TBasicBlock)item.Key, item.Value);
			return ((ICollection<KeyValuePair<TBasicBlock, int>>)this).Contains(item2);
		}

		bool IDictionary<IBasicBlock, int>.ContainsKey(IBasicBlock key) {
			return ContainsKey((TBasicBlock)key);
		}

		void ICollection<KeyValuePair<IBasicBlock, int>>.CopyTo(KeyValuePair<IBasicBlock, int>[] array, int arrayIndex) {
			var array2 = new KeyValuePair<TBasicBlock, int>[Count];
			((ICollection<KeyValuePair<TBasicBlock, int>>)this).CopyTo(array2, 0);
			for (int i = 0; i < array2.Length; i++)
				array[arrayIndex + i] = new KeyValuePair<IBasicBlock, int>(array2[i].Key, array2[i].Value);
		}

		IEnumerator<KeyValuePair<IBasicBlock, int>> IEnumerable<KeyValuePair<IBasicBlock, int>>.GetEnumerator() {
			return new Enumerator2(GetEnumerator());
		}

		bool IDictionary<IBasicBlock, int>.Remove(IBasicBlock key) {
			return Remove((TBasicBlock)key);
		}

		bool ICollection<KeyValuePair<IBasicBlock, int>>.Remove(KeyValuePair<IBasicBlock, int> item) {
			var item2 = new KeyValuePair<TBasicBlock, int>((TBasicBlock)item.Key, item.Value);
			return ((ICollection<KeyValuePair<TBasicBlock, int>>)this).Remove(item2);
		}

		bool IDictionary<IBasicBlock, int>.TryGetValue(IBasicBlock key, out int value) {
			return TryGetValue((TBasicBlock)key, out value);
		}

		private sealed class KeyCollection2 : ICollection<IBasicBlock> {
			private readonly BbRefDict<TBasicBlock> _dictionary;

			public KeyCollection2(BbRefDict<TBasicBlock> dictionary) {
				_dictionary = dictionary;
			}

			public int Count => _dictionary.Count;

			public bool IsReadOnly => true;

			public void Add(IBasicBlock item) {
				((ICollection<TBasicBlock>)_dictionary.Keys).Add((TBasicBlock)item);
			}

			public void Clear() {
				((ICollection<TBasicBlock>)_dictionary.Keys).Clear();
			}

			public bool Contains(IBasicBlock item) {
				return _dictionary.ContainsKey((TBasicBlock)item);
			}

			public void CopyTo(IBasicBlock[] array, int arrayIndex) {
				if (array is TBasicBlock[] array2) {
					_dictionary.Keys.CopyTo(array2, arrayIndex);
				}
				else {
					array2 = new TBasicBlock[Count];
					_dictionary.Keys.CopyTo(array2, 0);
					for (int i = 0; i < array2.Length; i++)
						array[arrayIndex + i] = array2[i];
				}
			}

			public IEnumerator<IBasicBlock> GetEnumerator() {
				return (IEnumerator<IBasicBlock>)(IEnumerator<TBasicBlock>)_dictionary.Keys.GetEnumerator();
			}

			public bool Remove(IBasicBlock item) {
				return ((ICollection<TBasicBlock>)_dictionary.Keys).Remove((TBasicBlock)item);
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return ((IEnumerable)_dictionary.Keys).GetEnumerator();
			}
		}

		private struct Enumerator2 : IEnumerator<KeyValuePair<IBasicBlock, int>> {
			private readonly IEnumerator<KeyValuePair<TBasicBlock, int>> _enumerator;

			public Enumerator2(IEnumerator<KeyValuePair<TBasicBlock, int>> enumerator) {
				_enumerator = enumerator;
			}

			public KeyValuePair<IBasicBlock, int> Current {
				get {
					var current = _enumerator.Current;
					return new KeyValuePair<IBasicBlock, int>(current.Key, current.Value);
				}
			}

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
