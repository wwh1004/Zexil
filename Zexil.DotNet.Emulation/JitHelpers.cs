using System;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation {
	internal static class JitHelpers {
		private const uint BIT_SBLK_GC_RESERVE = 0x20000000;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawData(this object obj) {
			return ref Unsafe.As<RawData>(obj).Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref byte GetRawSzArrayData(this Array array) {
			return ref Unsafe.As<RawSzArrayData>(array).Data;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetGCBit(object obj) {
			ref uint m_SyncBlock = ref Unsafe.As<byte, uint>(ref Unsafe.Subtract(ref GetRawData(obj), IntPtr.Size + sizeof(uint)));
			m_SyncBlock |= BIT_SBLK_GC_RESERVE;
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
