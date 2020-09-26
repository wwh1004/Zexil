using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Internal {
	internal static unsafe class GCHelpers {
		private const uint BIT_SBLK_GC_RESERVE = 0x20000000;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void SetGCBit(object obj) {
			ref uint m_SyncBlock = ref Unsafe.As<byte, uint>(ref Unsafe.Subtract(ref JitHelpers.GetRawData(obj), sizeof(nint) + sizeof(uint)));
			m_SyncBlock |= BIT_SBLK_GC_RESERVE;
		}
	}
}
