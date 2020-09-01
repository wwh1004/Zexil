using System;
using System.Collections;
using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis.Ast {
	/// <summary>
	/// Switch target list
	/// </summary>
	public sealed class TargetList : ITargetList, IList<BasicBlock> {
		private BasicBlock? _owner;
		private readonly List<BasicBlock> _targets;

		/// <summary>
		/// Owner of current instance
		/// </summary>
		public BasicBlock? Owner {
			get => _owner;
			internal set {
				if (!(value is null) && !(_owner is null))
					throw new InvalidOperationException($"{nameof(TargetList)} is already owned by another {nameof(BasicBlock)}.");

				if (!(value is null)) {
					_owner = value;
					foreach (var target in _targets)
						UpdateReferences(null, target);
				}
				else if (!(_owner is null)) {
					foreach (var target in _targets)
						UpdateReferences(target, null);
					_owner = null;
				}
			}
		}

		/// <summary>
		/// Constructor
		/// </summary>
		public TargetList() {
			_targets = new List<BasicBlock>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="capacity">Initial capacity</param>
		public TargetList(int capacity) {
			_targets = new List<BasicBlock>(capacity);
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="targets">Targets</param>
		public TargetList(IEnumerable<BasicBlock> targets) {
			_targets = new List<BasicBlock>(targets ?? throw new ArgumentNullException(nameof(targets)));
		}

		private void UpdateReferences(BasicBlock? oldValue, BasicBlock? newValue) {
			_owner?.UpdateReferences(oldValue, newValue);
		}

		#region ITargetList
		IBasicBlock? ITargetList.Owner { get => Owner; set => Owner = (BasicBlock?)value; }
		IBasicBlock IList<IBasicBlock>.this[int index] { get => this[index]; set => this[index] = (BasicBlock)value; }
		int IList<IBasicBlock>.IndexOf(IBasicBlock item) { return IndexOf((BasicBlock)item); }
		void IList<IBasicBlock>.Insert(int index, IBasicBlock item) { Insert(index, (BasicBlock)item); }
		void ICollection<IBasicBlock>.Add(IBasicBlock item) { Add((BasicBlock)item); }
		bool ICollection<IBasicBlock>.Contains(IBasicBlock item) { return Contains((BasicBlock)item); }
		void ICollection<IBasicBlock>.CopyTo(IBasicBlock[] array, int arrayIndex) { if (array is BasicBlock[] array2) { CopyTo(array2, arrayIndex); } else { array2 = new BasicBlock[Count]; CopyTo(array2, 0); for (int i = 0; i < array2.Length; i++) array[arrayIndex + i] = array2[i]; } }
		bool ICollection<IBasicBlock>.Remove(IBasicBlock item) { return Remove((BasicBlock)item); }
		IEnumerator<IBasicBlock> IEnumerable<IBasicBlock>.GetEnumerator() { return GetEnumerator(); }
		#endregion

		#region IList<BasicBlock>
		/// <inheritdoc />
		public int Count => _targets.Count;

		/// <inheritdoc />
		public bool IsReadOnly => ((IList<BasicBlock>)_targets).IsReadOnly;

		/// <inheritdoc />
		public BasicBlock this[int index] {
			get => _targets[index];
			set {
				var oldValue = _targets[index];
				_targets[index] = value;
				UpdateReferences(oldValue, value);
			}
		}

		/// <inheritdoc />
		public int IndexOf(BasicBlock item) {
			return _targets.IndexOf(item);
		}

		/// <inheritdoc />
		public void Insert(int index, BasicBlock item) {
			_targets.Insert(index, item);
			UpdateReferences(null, item);
		}

		/// <inheritdoc />
		public void RemoveAt(int index) {
			var oldValue = _targets[index];
			_targets.RemoveAt(index);
			UpdateReferences(oldValue, null);
		}

		/// <inheritdoc />
		public void Add(BasicBlock item) {
			_targets.Add(item);
			UpdateReferences(null, item);
		}

		/// <inheritdoc />
		public void Clear() {
			foreach (var target in _targets)
				UpdateReferences(target, null);
			_targets.Clear();
		}

		/// <inheritdoc />
		public bool Contains(BasicBlock item) {
			return _targets.Contains(item);
		}

		/// <inheritdoc />
		public void CopyTo(BasicBlock[] array, int arrayIndex) {
			_targets.CopyTo(array, arrayIndex);
		}

		/// <inheritdoc />
		public bool Remove(BasicBlock item) {
			if (_targets.Remove(item)) {
				UpdateReferences(item, null);
				return true;
			}
			else {
				return false;
			}
		}

		/// <inheritdoc />
		public IEnumerator<BasicBlock> GetEnumerator() {
			return ((IList<BasicBlock>)_targets).GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator() {
			return ((IList<BasicBlock>)_targets).GetEnumerator();
		}
		#endregion
	}
}
