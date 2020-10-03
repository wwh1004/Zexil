using System;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// Interpreter slot
	/// </summary>
	public unsafe struct InterpreterSlot : IEquatable<InterpreterSlot> {
		private long _value;
		private AnnotatedElementType _annotatedElementType;

		/// <summary>
		/// int
		/// </summary>
		public int I4 {
			get => (int)_value;
			set => _value = value;
		}

		/// <summary>
		/// uint
		/// </summary>
		public uint U4 {
			get => (uint)_value;
			set => _value = value;
		}

		/// <summary>
		/// long
		/// </summary>
		public long I8 {
			get => _value;
			set => _value = value;
		}

		/// <summary>
		/// ulong
		/// </summary>
		public ulong U8 {
			get => (ulong)_value;
			set => _value = (long)value;
		}

		/// <summary>
		/// native int
		/// </summary>
		public nint I {
			get => (nint)_value;
			set => _value = value;
		}

		/// <summary>
		/// native uint
		/// </summary>
		public nuint U {
			get => (nuint)_value;
			set => _value = (long)value;
		}

		/// <summary>
		/// float
		/// </summary>
		public float R4 {
			get => Unsafe.As<long, float>(ref _value);
			set => Unsafe.As<long, float>(ref _value) = value;
		}

		/// <summary>
		/// double
		/// </summary>
		public double R8 {
			get => Unsafe.As<long, double>(ref _value);
			set => Unsafe.As<long, double>(ref _value) = value;
		}

		/// <summary>
		/// ElementType
		/// </summary>
		public ElementType ElementType {
			get => (ElementType)_annotatedElementType;
			set => _annotatedElementType = (AnnotatedElementType)value;
		}

		/// <summary>
		/// ElementType with annotation
		/// </summary>
		public AnnotatedElementType AnnotatedElementType {
			get => _annotatedElementType;
			set => _annotatedElementType = value;
		}

		/// <summary>
		/// Annotation
		/// </summary>
		public AnnotatedElementType Annotation {
			get => _annotatedElementType & (AnnotatedElementType)0xFFFFFF00;
			set => _annotatedElementType = (_annotatedElementType & (AnnotatedElementType)0xFF) | (value & (AnnotatedElementType)0xFFFFFF00);
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I4"/>
		/// </summary>
		public bool IsI4 => ElementType == ElementType.I4;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I8"/>
		/// </summary>
		public bool IsI8 => ElementType == ElementType.I8;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I"/>
		/// </summary>
		public bool IsI => ElementType == ElementType.I;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.ByRef"/>
		/// </summary>
		public bool IsByRef => ElementType == ElementType.ByRef;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.R4"/>
		/// </summary>
		public bool IsR4 => ElementType == ElementType.R4;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.R8"/>
		/// </summary>
		public bool IsR8 => ElementType == ElementType.R8;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.Class"/>
		/// </summary>
		public bool IsClass => ElementType == ElementType.Class;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I4"/> or <see cref="ElementType.I8"/> or <see cref="ElementType.I"/> or <see cref="ElementType.ByRef"/>
		/// or <see cref="ElementType.R4"/> or <see cref="ElementType.R8"/> or <see cref="ElementType.TypedByRef"/>
		/// </summary>
		public bool IsValueType => ElementType != ElementType.Class;

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.TypedByRef"/>
		/// </summary>
		public bool IsTypedRef => ElementType == ElementType.TypedByRef;

		/// <summary>
		/// Equals
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(in InterpreterSlot other) {
			return other._value == _value && other._annotatedElementType == _annotatedElementType;
		}

		/// <inheritdoc />
		public override string ToString() {
			switch (ElementType) {
			case ElementType.I4:
				return $"{ElementType}, {Annotation}, 0x{I4:X4}, {I4}";
			case ElementType.I8:
			case ElementType.ValueType:
				return $"{ElementType}, {Annotation}, 0x{I8:X8}, {I8}";
			case ElementType.I:
				return $"{ElementType}, {Annotation}, 0x{(sizeof(nint) == 4 ? I4.ToString("X4") : I8.ToString("X16"))}, {I}";
			case ElementType.R4:
				return $"{ElementType}, {Annotation}, {R4}";
			case ElementType.R8:
				return $"{ElementType}, {Annotation}, {R8}";
			case ElementType.ByRef:
			case ElementType.Class:
				return $"{ElementType}, {Annotation}, 0x{(sizeof(nint) == 4 ? I4.ToString("X4") : I8.ToString("X16"))}";
			default:
				throw new InvalidOperationException();
			}
			// TODO: supports typedref
		}

		bool IEquatable<InterpreterSlot>.Equals(InterpreterSlot other) {
			throw new NotImplementedException();
		}
	}
}
