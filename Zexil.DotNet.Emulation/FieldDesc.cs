using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime field
	/// </summary>
	public sealed unsafe class FieldDesc {
		private readonly FieldInfo _internalValue;
		private readonly TypeDesc _type;
		private readonly uint _offset;

		/// <summary>
		/// Internal value
		/// </summary>
		public FieldInfo InternalValue => _internalValue;

		/// <summary>
		/// Field type
		/// </summary>
		public TypeDesc Type => _type;

		/// <summary>
		/// Field offset
		/// </summary>
		public uint Offset => _offset;

		/// <summary>
		/// Is a static field
		/// </summary>
		public bool IsStatic => _offset == uint.MaxValue;

		internal FieldDesc(ExecutionEngine runtime, FieldInfo field) {
			_internalValue = field;
			_type = runtime.ResolveType(field.FieldType);
			_offset = !field.IsStatic ? Unsafe.GetFieldOffset((void*)field.FieldHandle.Value) : uint.MaxValue;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _internalValue.ToString();
		}
	}
}
