using System;
using System.Reflection;
using System.Reflection.Emit;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Unsafe code helper
	/// </summary>
	internal static unsafe class Unsafe {
		private delegate object AllocateObjectDelegate(IntPtr typeHandle);

		private static readonly AllocateObjectDelegate _allocateObject = CreateAllocateObject();

		/// <summary>
		/// Gets type handle.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static void* GetTypeHandle<T>() {
			return (void*)typeof(T).TypeHandle.Value;
		}

		/// <summary>
		/// Gets type size in memory (includes object header size).
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static uint GetTypeSize<T>() {
			return ((uint*)GetTypeHandle<T>())[1];
		}

		/// <summary>
		/// Casts a value to specified type.
		/// </summary>
		/// <typeparam name="TFrom"></typeparam>
		/// <typeparam name="TTo"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static TTo As<TFrom, TTo>(TFrom value) where TFrom : class where TTo : class {
			return ToObject<TTo>(ToPointer(value));
		}

		/// <summary>
		/// Gets the unmanaged pointer of a value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static void* ToPointer<T>(T value) where T : class {
			var @ref = __makeref(value);
			return **(void***)&@ref;
		}

		/// <summary>
		/// Gets the unmanaged pointer of a value.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="value"></param>
		/// <returns></returns>
		public static void* ToPointer<T>(ref T value) where T : struct {
			var @ref = __makeref(value);
			return *(void**)&@ref;
		}

		/// <summary>
		/// Gets the object from an unmanaged pointer.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="ptr"></param>
		/// <returns></returns>
		public static T ToObject<T>(void* ptr) where T : class {
			var dummy = default(T);
			var @ref = __makeref(dummy);
			*(void**)&@ref = &ptr;
			return __refvalue(@ref, T);
		}

		/// <summary>
		/// Allocates an object without running any constructor.
		/// Note that if you just want to create a dummy for some special purposes, please call <see cref="GC.SuppressFinalize(object)"/> for it.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public static T AllocateObject<T>() {
			return (T)AllocateObject(GetTypeHandle<T>());
		}

		/// <summary>
		/// Allocates an object without running any constructor.
		/// Note that if you just want to create a dummy for some special purposes, please call <see cref="GC.SuppressFinalize(object)"/> for it.
		/// </summary>
		/// <returns></returns>
		public static object AllocateObject(void* typeHandle) {
			return _allocateObject((IntPtr)typeHandle);
		}

		/// <summary>
		/// Gets field offset in memory (excludes object header size).
		/// </summary>
		/// <param name="fieldHandle"></param>
		/// <returns></returns>
		public static uint GetFieldOffset(void* fieldHandle) {
			return *(uint*)((byte*)fieldHandle + sizeof(void*) + 4) & 0x7FFFFFF;
		}

		private static AllocateObjectDelegate CreateAllocateObject() {
			var allocateImpl = Environment.Version.Major == 2
				? typeof(RuntimeTypeHandle).GetMethod("Allocate", BindingFlags.NonPublic | BindingFlags.Instance)
				: typeof(object).Module.GetType("System.StubHelpers.StubHelpers").GetMethod("AllocateInternal", BindingFlags.NonPublic | BindingFlags.Static);
			var allocate = new DynamicMethod("", typeof(object), new[] { typeof(IntPtr) }, true);
			var generator = allocate.GetILGenerator();
			generator.Emit(OpCodes.Ldarg_0);
			generator.Emit(OpCodes.Call, allocateImpl);
			generator.Emit(OpCodes.Ret);
			return (AllocateObjectDelegate)allocate.CreateDelegate(typeof(AllocateObjectDelegate));
		}
	}
}
