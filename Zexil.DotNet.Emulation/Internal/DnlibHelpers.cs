using System;
using dnlib.DotNet;

namespace Zexil.DotNet.Emulation.Internal {
	internal static class DnlibHelpers {
		public static Type TypeSigToReflTypeLocal(MethodDesc method, TypeSig typeSig) {
			return TypeSigToReflTypeLocalImpl(method, typeSig.RemovePinnedAndModifiers());
		}

		private static Type TypeSigToReflTypeLocalImpl(MethodDesc method, TypeSig typeSig) {
			switch (typeSig.ElementType) {
			case dnlib.DotNet.ElementType.Void: return typeof(void);
			case dnlib.DotNet.ElementType.Boolean: return typeof(bool);
			case dnlib.DotNet.ElementType.Char: return typeof(char);
			case dnlib.DotNet.ElementType.I1: return typeof(sbyte);
			case dnlib.DotNet.ElementType.U1: return typeof(byte);
			case dnlib.DotNet.ElementType.I2: return typeof(short);
			case dnlib.DotNet.ElementType.U2: return typeof(ushort);
			case dnlib.DotNet.ElementType.I4: return typeof(int);
			case dnlib.DotNet.ElementType.U4: return typeof(uint);
			case dnlib.DotNet.ElementType.I8: return typeof(long);
			case dnlib.DotNet.ElementType.U8: return typeof(ulong);
			case dnlib.DotNet.ElementType.R4: return typeof(float);
			case dnlib.DotNet.ElementType.R8: return typeof(double);
			case dnlib.DotNet.ElementType.String: return typeof(string);
			case dnlib.DotNet.ElementType.Ptr: return TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakePointerType();
			case dnlib.DotNet.ElementType.ByRef: return TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeByRefType();
			case dnlib.DotNet.ElementType.ValueType:
			case dnlib.DotNet.ElementType.Class: return method.DeclaringType.Module.ResolveReflType(((TypeDefOrRefSig)typeSig).TypeDefOrRef.MDToken.ToInt32());
			case dnlib.DotNet.ElementType.Var: return method.DeclaringType.Instantiation[((GenericVar)typeSig).Number]._reflType;
			case dnlib.DotNet.ElementType.Array: return TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeArrayType((int)((ArraySig)typeSig).Rank);
			case dnlib.DotNet.ElementType.TypedByRef: return TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeByRefType();
			case dnlib.DotNet.ElementType.I: return typeof(IntPtr);
			case dnlib.DotNet.ElementType.U: return typeof(UIntPtr);
			case dnlib.DotNet.ElementType.R: return typeof(double);
			case dnlib.DotNet.ElementType.FnPtr: return typeof(IntPtr);
			case dnlib.DotNet.ElementType.Object: return typeof(object);
			case dnlib.DotNet.ElementType.SZArray: return TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeArrayType();
			case dnlib.DotNet.ElementType.MVar: return method.Instantiation[((GenericMVar)typeSig).Number]._reflType;
			case dnlib.DotNet.ElementType.End:
			case dnlib.DotNet.ElementType.GenericInst:
			case dnlib.DotNet.ElementType.ValueArray:
			case dnlib.DotNet.ElementType.CModReqd:
			case dnlib.DotNet.ElementType.CModOpt:
			case dnlib.DotNet.ElementType.Internal:
			case dnlib.DotNet.ElementType.Module:
			case dnlib.DotNet.ElementType.Sentinel:
			case dnlib.DotNet.ElementType.Pinned: throw new NotSupportedException();
			default: throw new InvalidOperationException("Unreachable");
			}
		}
	}
}
