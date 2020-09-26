using System;
using dnlib.DotNet;

namespace Zexil.DotNet.Emulation.Internal {
	internal static class DnlibHelpers {
		public static Type TypeSigToReflTypeLocal(MethodDesc method, TypeSig typeSig) {
			return TypeSigToReflTypeLocalImpl(method, typeSig.RemovePinnedAndModifiers());
		}

		private static Type TypeSigToReflTypeLocalImpl(MethodDesc method, TypeSig typeSig) {
			return typeSig.ElementType switch
			{
				dnlib.DotNet.ElementType.Void => typeof(void),
				dnlib.DotNet.ElementType.Boolean => typeof(bool),
				dnlib.DotNet.ElementType.Char => typeof(char),
				dnlib.DotNet.ElementType.I1 => typeof(sbyte),
				dnlib.DotNet.ElementType.U1 => typeof(byte),
				dnlib.DotNet.ElementType.I2 => typeof(short),
				dnlib.DotNet.ElementType.U2 => typeof(ushort),
				dnlib.DotNet.ElementType.I4 => typeof(int),
				dnlib.DotNet.ElementType.U4 => typeof(uint),
				dnlib.DotNet.ElementType.I8 => typeof(long),
				dnlib.DotNet.ElementType.U8 => typeof(ulong),
				dnlib.DotNet.ElementType.R4 => typeof(float),
				dnlib.DotNet.ElementType.R8 => typeof(double),
				dnlib.DotNet.ElementType.String => typeof(string),
				dnlib.DotNet.ElementType.Ptr => TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakePointerType(),
				dnlib.DotNet.ElementType.ByRef => TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeByRefType(),
				dnlib.DotNet.ElementType.ValueType or dnlib.DotNet.ElementType.Class => method.DeclaringType.Module.ResolveReflType(((TypeDefOrRefSig)typeSig).TypeDefOrRef.MDToken.ToInt32()),
				dnlib.DotNet.ElementType.Var => method.DeclaringType.Instantiation[((GenericVar)typeSig).Number]._reflType,
				dnlib.DotNet.ElementType.Array => TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeArrayType((int)((ArraySig)typeSig).Rank),
				dnlib.DotNet.ElementType.TypedByRef => TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeByRefType(),
				dnlib.DotNet.ElementType.I => typeof(nint),
				dnlib.DotNet.ElementType.U => typeof(nuint),
				dnlib.DotNet.ElementType.R => typeof(double),
				dnlib.DotNet.ElementType.FnPtr => typeof(nint),
				dnlib.DotNet.ElementType.Object => typeof(object),
				dnlib.DotNet.ElementType.SZArray => TypeSigToReflTypeLocalImpl(method, typeSig.Next).MakeArrayType(),
				dnlib.DotNet.ElementType.MVar => method.Instantiation[((GenericMVar)typeSig).Number]._reflType,
				_ => throw new NotSupportedException()
			};
		}
	}
}
