using System;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Internal {
	internal static class JitHelpers {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawData(this object obj) {
			return ref Unsafe.As<RawData>(obj).Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint GetSZArrayLength(this Array array) {
			return (uint)Unsafe.As<RawSZArrayData>(array).Count;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawSZArrayData(this Array array) {
			return ref Unsafe.As<RawSZArrayData>(array).Data;
		}

#pragma warning disable CS0649
		private sealed class RawData {
			public byte Data;
		}

		private sealed class RawSZArrayData {
			public IntPtr Count;
			public byte Data;
		}
#pragma warning restore CS0649
	}
}
