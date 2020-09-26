using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// Interpreter slot
	/// </summary>
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
	public unsafe struct InterpreterSlot {
		private long _value;
		private AnnotatedElementType _annotatedElementType;

		public int I4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (int)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		public uint U4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (uint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		public long I8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		public ulong U8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ulong)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (long)value;
		}

		public nint I {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (nint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = value;
		}

		public nuint U {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (nuint)_value;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _value = (long)value;
		}

		public float R4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Unsafe.As<long, float>(ref _value);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Unsafe.As<long, float>(ref _value) = value;
		}

		public double R8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => Unsafe.As<long, double>(ref _value);
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => Unsafe.As<long, double>(ref _value) = value;
		}

		public AnnotatedElementType AnnotatedElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _annotatedElementType;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _annotatedElementType = value;
		}

		public ElementType ElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ElementType)_annotatedElementType;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _annotatedElementType = (AnnotatedElementType)value;
		}

		public bool IsI4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I4;
		}

		public bool IsI8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I8;
		}

		public bool IsI {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.I;
		}

		public bool IsByRef {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.ByRef;
		}

		public bool IsR4 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.R4;
		}

		public bool IsR8 {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.R8;
		}
	}
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
}
