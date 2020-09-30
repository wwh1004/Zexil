using System;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// Interpreter slot
	/// </summary>
	public unsafe struct InterpreterSlot {
		private long _value;
		private AnnotatedElementType _annotatedElementType;

		/// <summary>
		/// int
		/// </summary>
		public int I4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (int)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		/// <summary>
		/// uint
		/// </summary>
		public uint U4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (uint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		/// <summary>
		/// long
		/// </summary>
		public long I8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		/// <summary>
		/// ulong
		/// </summary>
		public ulong U8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ulong)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (long)value;
		}

		/// <summary>
		/// native int
		/// </summary>
		public nint I {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (nint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		/// <summary>
		/// native uint
		/// </summary>
		public nuint U {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (nuint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (long)value;
		}

		/// <summary>
		/// float
		/// </summary>
		public float R4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Unsafe.As<long, float>(ref _value);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Unsafe.As<long, float>(ref _value) = value;
		}

		/// <summary>
		/// double
		/// </summary>
		public double R8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Unsafe.As<long, double>(ref _value);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Unsafe.As<long, double>(ref _value) = value;
		}

		/// <summary>
		/// ElementType
		/// </summary>
		public ElementType ElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ElementType)_annotatedElementType;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _annotatedElementType = (AnnotatedElementType)value;
		}

		/// <summary>
		/// ElementType with annotation
		/// </summary>
		public AnnotatedElementType AnnotatedElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _annotatedElementType;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _annotatedElementType = value;
		}

		/// <summary>
		/// Annotation
		/// </summary>
		public AnnotatedElementType Annotation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _annotatedElementType & (AnnotatedElementType)0xFFFFFF00;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _annotatedElementType = (_annotatedElementType & (AnnotatedElementType)0xFF) | (value & (AnnotatedElementType)0xFFFFFF00);
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I4"/>
		/// </summary>
		public bool IsI4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I4;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I8"/>
		/// </summary>
		public bool IsI8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I8;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I"/>
		/// </summary>
		public bool IsI {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.ByRef"/>
		/// </summary>
		public bool IsByRef {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.ByRef;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.R4"/>
		/// </summary>
		public bool IsR4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.R4;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.R8"/>
		/// </summary>
		public bool IsR8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.R8;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.Class"/>
		/// </summary>
		public bool IsClass {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.Class;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.I4"/> or <see cref="ElementType.I8"/> or <see cref="ElementType.I"/> or <see cref="ElementType.ByRef"/>
		/// or <see cref="ElementType.R4"/> or <see cref="ElementType.R8"/> or <see cref="ElementType.TypedByRef"/>
		/// </summary>
		public bool IsValueType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType != ElementType.Class;
		}

		/// <summary>
		/// Is stack-normalized <see cref="ElementType.TypedByRef"/>
		/// </summary>
		public bool IsTypedRef {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.TypedByRef;
		}

		/// <inheritdoc />
		public override string ToString() {
			return ElementType switch
			{
				ElementType.I4 => $"{ElementType}, {Annotation}, 0x{I4:X4}, {I4}",
				ElementType.I8 or ElementType.ValueArray => $"{ElementType}, {Annotation}, 0x{I8:X8}, {I8}",
				ElementType.I or ElementType.ByRef or ElementType.Class => $"{ElementType}, {Annotation}, 0x{(sizeof(nint) == 4 ? I4.ToString("X4") : I8.ToString("X16"))}, {I}",
				ElementType.R4 => $"{ElementType}, {Annotation}, {R4}",
				ElementType.R8 => $"{ElementType}, {Annotation}, {R8}",
				_ => throw new InvalidOperationException(),
			};
			// TODO: supports typedref
		}
	}
}
