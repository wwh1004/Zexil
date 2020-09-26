using System;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Zexil.DotNet.Emulation.Internal {
	/// <summary>
	/// <see cref="GCHandle"/> patcher that patches CLR to allow to pin any objects.
	/// </summary>
	internal static unsafe class GCHandlePatcher {
		private const uint STATE_UNKNOWN = 0x00000000;
		private const uint STATE_ISPINNABLE = 0x00000001;
		private const uint STATE_INTERNALALLOC = 0x00000002;
		private const uint STATE_MASK = 0x7FFFFFFF;
		private const uint STATE_PATCHED = 0x80000000;

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool VirtualProtect(void* lpAddress, uint dwSize, uint flNewProtect, uint* lpflOldProtect);

		private static byte* _position;
		private static byte[] _original;
		private static uint _state;

		/// <summary>
		/// Whether CLR is patched.
		/// </summary>
		public static bool IsPatched => (_state & STATE_PATCHED) == STATE_PATCHED;

		/// <summary>
		/// Patches the CLR internal function for freely pinning an object.
		/// </summary>
		/// <returns></returns>
		public static bool Patch() {
			if (IsPatched)
				return false;

			byte* position = null;
			uint state = _state & STATE_MASK;
			if (state == STATE_UNKNOWN) {
				position = TryGetIsPinnablePosition();
				if (!(position is null)) {
					state = STATE_ISPINNABLE;
					goto state_found;
				}
				position = TryGetInternalAllocPosition();
				if (!(position is null)) {
					state = STATE_INTERNALALLOC;
					goto state_found;
				}
				return false;
			}
		state_found:
			switch (state) {
			case STATE_ISPINNABLE: {
				byte* pIsPinnable = position;
				uint oldProtect;
				if (!VirtualProtect(pIsPinnable, 3, 0x40, &oldProtect))
					return false;
				_original = new byte[3];
				_original[0] = pIsPinnable[0];
				_original[1] = pIsPinnable[1];
				_original[2] = pIsPinnable[2];
				pIsPinnable[0] = 0x0C;
				pIsPinnable[1] = 0x01;
				// or al, 0x1
				pIsPinnable[2] = 0xC3;
				// ret
				if (!VirtualProtect(pIsPinnable, 3, oldProtect, &oldProtect))
					return false;
				break;
			}
			case STATE_INTERNALALLOC: {
				byte* pCmpReg03 = position;
				uint oldProtect;
				if (!VirtualProtect(pCmpReg03, 1, 0x40, &oldProtect))
					return false;
				_original = new byte[1] { 0x03 };
				pCmpReg03[0] = 0xAA;
				// cmp reg, 0x03 ->  cmp reg, 0xAA
				if (!VirtualProtect(pCmpReg03, 1, oldProtect, &oldProtect))
					return false;
				break;
			}
			default:
				throw new InvalidOperationException();
			}
			_position = position;
			_state = STATE_PATCHED | state;
			return true;
		}

		private static byte* TryGetIsPinnablePosition() {
			var isPinnable = typeof(Marshal).GetMethod("IsPinnable", BindingFlags.NonPublic | BindingFlags.Static);
			if (isPinnable is null)
				return null;
			byte* pIsPinnable = (byte*)isPinnable.MethodHandle.GetFunctionPointer();
			return pIsPinnable;
		}

		private static byte* TryGetInternalAllocPosition() {
			var internalAlloc = typeof(GCHandle).GetMethod("InternalAlloc", BindingFlags.NonPublic | BindingFlags.Static);
			if (internalAlloc is null)
				return null;
			byte* pInternalAlloc = (byte*)internalAlloc.MethodHandle.GetFunctionPointer();

			for (int i = 0; i < 0x100; i++) {
				byte* p = pInternalAlloc + i;
				if (p[0] != 0x83 || 0xF0 > p[1] || p[1] > 0xFF || p[2] != 0x03)
					continue;
				// cmp reg, 0x03 (#define HNDTYPE_PINNED == 0x03)
				return p + 2;
			}
			return null;
		}

		/// <summary>
		/// Restore the patch
		/// </summary>
		/// <returns></returns>
		public static bool Restore() {
			if (!IsPatched)
				return false;

			uint oldProtect;
			if (!VirtualProtect(_position, (uint)_original.Length, 0x40, &oldProtect))
				return false;
			for (int i = 0; i < _original.Length; i++)
				_position[i] = _original[i];
			if (!VirtualProtect(_position, (uint)_original.Length, oldProtect, &oldProtect))
				return false;

			_state &= ~STATE_PATCHED;
			return true;
		}
	}
}
