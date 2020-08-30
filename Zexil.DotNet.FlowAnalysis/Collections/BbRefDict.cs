using System;
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
			throw new NotImplementedException();
		}

		bool ICollection<KeyValuePair<IBasicBlock, int>>.Contains(KeyValuePair<IBasicBlock, int> item) {
			throw new NotImplementedException();
		}

		bool IDictionary<IBasicBlock, int>.ContainsKey(IBasicBlock key) {
			return ContainsKey((TBasicBlock)key);
		}

		void ICollection<KeyValuePair<IBasicBlock, int>>.CopyTo(KeyValuePair<IBasicBlock, int>[] array, int arrayIndex) {
			throw new NotImplementedException();
		}

		IEnumerator<KeyValuePair<IBasicBlock, int>> IEnumerable<KeyValuePair<IBasicBlock, int>>.GetEnumerator() {
			return new Enumerator2(GetEnumerator());
		}

		bool IDictionary<IBasicBlock, int>.Remove(IBasicBlock key) {
			return Remove((TBasicBlock)key);
		}

		bool ICollection<KeyValuePair<IBasicBlock, int>>.Remove(KeyValuePair<IBasicBlock, int> item) {
			throw new NotImplementedException();
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
				throw new NotSupportedException();
			}

			public void Clear() {
				throw new NotSupportedException();
			}

			public bool Contains(IBasicBlock item) {
				return _dictionary.ContainsKey((TBasicBlock)item);
			}

			public void CopyTo(IBasicBlock[] array, int arrayIndex) {
				throw new NotImplementedException();
			}

			public IEnumerator<IBasicBlock> GetEnumerator() {
				return (IEnumerator<IBasicBlock>)(IEnumerator<TBasicBlock>)_dictionary.Keys.GetEnumerator();
			}

			public bool Remove(IBasicBlock item) {
				throw new NotSupportedException();
			}

			IEnumerator IEnumerable.GetEnumerator() {
				return _dictionary.Keys.GetEnumerator();
			}
		}

		private struct Enumerator2 : IEnumerator<KeyValuePair<IBasicBlock, int>> {
			private readonly Enumerator _enumerator;

			public Enumerator2(in Enumerator enumerator) {
				_enumerator = enumerator;
			}

			public KeyValuePair<IBasicBlock, int> Current {
				get {
					var current = _enumerator.Current;
					return new KeyValuePair<IBasicBlock, int>(current.Key, current.Value);
				}
			}

			object IEnumerator.Current => _enumerator.Current;

			public void Dispose() {
				_enumerator.Dispose();
			}

			public bool MoveNext() {
				return _enumerator.MoveNext();
			}

			public void Reset() {
				((IEnumerator)_enumerator).Reset();
			}
		}
	}
}
