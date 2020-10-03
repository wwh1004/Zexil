using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace Zexil.DotNet.Emulation.Internal {
	internal static unsafe class GCHelpers {
		private delegate object AllocateObjectDelegate(nint typeHandle);

		private const uint BIT_SBLK_GC_RESERVE = 0x20000000;
		private static readonly AllocateObjectDelegate _allocateObject = CreateAllocateObject();

		public static void Initialize() {
			if (!GCHandlePatcher.IsPatched && !GCHandlePatcher.Patch())
				throw new NotSupportedException($"{nameof(GCHandlePatcher)} should be updated");
		}

		public static void SetGCBit(object obj) {
			ref uint m_SyncBlock = ref Unsafe.As<byte, uint>(ref Unsafe.Subtract(ref Unsafe.As<RawData>(obj).Data, sizeof(nint) * 2));
			m_SyncBlock |= BIT_SBLK_GC_RESERVE;
		}

		/// <summary>
		/// Allocates an object without running any constructor.
		/// Note that if you just want to create a dummy for some special purposes, please call <see cref="GC.SuppressFinalize(object)"/> for it.
		/// </summary>
		/// <param name="typeHandle"></param>
		/// <returns></returns>
		public static object AllocateObject(nint typeHandle) {
			return _allocateObject(typeHandle);
		}

		private static AllocateObjectDelegate CreateAllocateObject() {
			if (CLREnvironment.IsFramework2x) {
				var allocateImpl = typeof(RuntimeTypeHandle).GetMethod("Allocate", BindingFlags.NonPublic | BindingFlags.Instance);
				var allocate = new DynamicMethod("", typeof(object), new[] { typeof(nint) }, typeof(GCHelpers), true);
				var generator = allocate.GetILGenerator();
				generator.Emit(OpCodes.Ldarga_S, 0);
				generator.Emit(OpCodes.Call, allocateImpl);
				generator.Emit(OpCodes.Ret);
				return (AllocateObjectDelegate)allocate.CreateDelegate(typeof(AllocateObjectDelegate));
			}
			else {
				var allocateImpl = typeof(object).Module.GetType("System.StubHelpers.StubHelpers").GetMethod("AllocateInternal", BindingFlags.NonPublic | BindingFlags.Static);
				var allocate = new DynamicMethod("", typeof(object), new[] { typeof(nint) }, typeof(GCHelpers), true);
				var generator = allocate.GetILGenerator();
				generator.Emit(OpCodes.Ldarg_0);
				generator.Emit(OpCodes.Call, allocateImpl);
				generator.Emit(OpCodes.Ret);
				return (AllocateObjectDelegate)allocate.CreateDelegate(typeof(AllocateObjectDelegate));
			}
		}

		private sealed class RawData {
			public byte Data;
		}
	}
}
