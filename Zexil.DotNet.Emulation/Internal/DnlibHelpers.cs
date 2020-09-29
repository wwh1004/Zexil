using System;
using dnlib.DotNet;

namespace Zexil.DotNet.Emulation.Internal {
	internal static class DnlibHelpers {
		public static TypeDesc ResolveTypeSig(MethodDesc method, TypeSig typeSig) {
			return ResolveTypeSigImpl(method, typeSig.RemovePinnedAndModifiers());
		}

		private static TypeDesc ResolveTypeSigImpl(MethodDesc method, TypeSig typeSig) {
			return typeSig.ElementType switch
			{
				dnlib.DotNet.ElementType.Void => method.ExecutionEngine.ResolveType(typeof(void)),
				dnlib.DotNet.ElementType.Boolean => method.ExecutionEngine.ResolveType(typeof(bool)),
				dnlib.DotNet.ElementType.Char => method.ExecutionEngine.ResolveType(typeof(char)),
				dnlib.DotNet.ElementType.I1 => method.ExecutionEngine.ResolveType(typeof(sbyte)),
				dnlib.DotNet.ElementType.U1 => method.ExecutionEngine.ResolveType(typeof(byte)),
				dnlib.DotNet.ElementType.I2 => method.ExecutionEngine.ResolveType(typeof(short)),
				dnlib.DotNet.ElementType.U2 => method.ExecutionEngine.ResolveType(typeof(ushort)),
				dnlib.DotNet.ElementType.I4 => method.ExecutionEngine.ResolveType(typeof(int)),
				dnlib.DotNet.ElementType.U4 => method.ExecutionEngine.ResolveType(typeof(uint)),
				dnlib.DotNet.ElementType.I8 => method.ExecutionEngine.ResolveType(typeof(long)),
				dnlib.DotNet.ElementType.U8 => method.ExecutionEngine.ResolveType(typeof(ulong)),
				dnlib.DotNet.ElementType.R4 => method.ExecutionEngine.ResolveType(typeof(float)),
				dnlib.DotNet.ElementType.R8 => method.ExecutionEngine.ResolveType(typeof(double)),
				dnlib.DotNet.ElementType.String => method.ExecutionEngine.ResolveType(typeof(string)),
				dnlib.DotNet.ElementType.Ptr => ResolveTypeSigImpl(method, typeSig.Next).MakePointerType(),
				dnlib.DotNet.ElementType.ByRef => ResolveTypeSigImpl(method, typeSig.Next).MakeByRefType(),
				dnlib.DotNet.ElementType.ValueType or dnlib.DotNet.ElementType.Class => method.Module.ResolveType(((TypeDefOrRefSig)typeSig).TypeDefOrRef.MDToken.ToInt32()),
				dnlib.DotNet.ElementType.Var => method.DeclaringType.Instantiation[((GenericVar)typeSig).Number],
				dnlib.DotNet.ElementType.Array => ResolveTypeSigImpl(method, typeSig.Next).MakeArrayType((int)((ArraySig)typeSig).Rank),
				dnlib.DotNet.ElementType.TypedByRef => ResolveTypeSigImpl(method, typeSig.Next).MakeByRefType(),
				dnlib.DotNet.ElementType.I => method.ExecutionEngine.ResolveType(typeof(nint)),
				dnlib.DotNet.ElementType.U => method.ExecutionEngine.ResolveType(typeof(nuint)),
				dnlib.DotNet.ElementType.R => method.ExecutionEngine.ResolveType(typeof(double)),
				dnlib.DotNet.ElementType.FnPtr => method.ExecutionEngine.ResolveType(typeof(nint)),
				dnlib.DotNet.ElementType.Object => method.ExecutionEngine.ResolveType(typeof(object)),
				dnlib.DotNet.ElementType.SZArray => ResolveTypeSigImpl(method, typeSig.Next).MakeArrayType(),
				dnlib.DotNet.ElementType.MVar => method.Instantiation[((GenericMVar)typeSig).Number],
				_ => throw new NotSupportedException()
			};
		}
	}
}
