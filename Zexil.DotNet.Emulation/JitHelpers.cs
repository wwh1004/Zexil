using System;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation {
	internal static class JitHelpers {
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawData(this object obj) {
			return ref Unsafe.As<RawData>(obj).Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawSzArrayData(this Array array) {
			return ref Unsafe.As<RawSzArrayData>(array).Data;
		}

		private sealed class RawData {
			public byte Data;
		}

		private class RawSzArrayData {
			public IntPtr Count;
			public byte Data;
		}
	}
}
