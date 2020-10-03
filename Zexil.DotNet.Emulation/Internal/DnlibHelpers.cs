using System;
using System.Linq;
using dnlib.DotNet;

namespace Zexil.DotNet.Emulation.Internal {
	internal static class DnlibHelpers {
		public static TypeDesc TryResolveTypeSig(ExecutionEngine executionEngine, MethodDesc method, TypeSig typeSig) {
			return TryResolveTypeSigImpl(executionEngine, method, typeSig.RemovePinnedAndModifiers());
		}

		private static TypeDesc TryResolveTypeSigImpl(ExecutionEngine executionEngine, MethodDesc method, TypeSig typeSig) {
			switch (typeSig.ElementType) {
			case dnlib.DotNet.ElementType.Void:
				return executionEngine.ResolveType(typeof(void));
			case dnlib.DotNet.ElementType.Boolean:
				return executionEngine.ResolveType(typeof(bool));
			case dnlib.DotNet.ElementType.Char:
				return executionEngine.ResolveType(typeof(char));
			case dnlib.DotNet.ElementType.I1:
				return executionEngine.ResolveType(typeof(sbyte));
			case dnlib.DotNet.ElementType.U1:
				return executionEngine.ResolveType(typeof(byte));
			case dnlib.DotNet.ElementType.I2:
				return executionEngine.ResolveType(typeof(short));
			case dnlib.DotNet.ElementType.U2:
				return executionEngine.ResolveType(typeof(ushort));
			case dnlib.DotNet.ElementType.I4:
				return executionEngine.ResolveType(typeof(int));
			case dnlib.DotNet.ElementType.U4:
				return executionEngine.ResolveType(typeof(uint));
			case dnlib.DotNet.ElementType.I8:
				return executionEngine.ResolveType(typeof(long));
			case dnlib.DotNet.ElementType.U8:
				return executionEngine.ResolveType(typeof(ulong));
			case dnlib.DotNet.ElementType.R4:
				return executionEngine.ResolveType(typeof(float));
			case dnlib.DotNet.ElementType.R8:
				return executionEngine.ResolveType(typeof(double));
			case dnlib.DotNet.ElementType.String:
				return executionEngine.ResolveType(typeof(string));
			case dnlib.DotNet.ElementType.Ptr:
				return TryResolveTypeSigImpl(executionEngine, method, typeSig.Next)?.MakePointerType();
			case dnlib.DotNet.ElementType.ByRef:
				return TryResolveTypeSigImpl(executionEngine, method, typeSig.Next)?.MakeByRefType();
			case dnlib.DotNet.ElementType.ValueType:
			case dnlib.DotNet.ElementType.Class:
				return method?.Module.ResolveType(((TypeDefOrRefSig)typeSig).TypeDefOrRef.MDToken.ToInt32());
			case dnlib.DotNet.ElementType.Var:
				return method?.DeclaringType.Instantiation[((GenericVar)typeSig).Number];
			case dnlib.DotNet.ElementType.Array:
				return TryResolveTypeSigImpl(executionEngine, method, typeSig.Next)?.MakeArrayType((int)((ArraySig)typeSig).Rank);
			case dnlib.DotNet.ElementType.GenericInst: {
				var genericInstSig = (GenericInstSig)typeSig;
				var genericType = TryResolveTypeSigImpl(executionEngine, method, genericInstSig.GenericType);
				if (genericType is null)
					return null;

				var genericArguments = genericInstSig.GenericArguments.Select(t => TryResolveTypeSigImpl(executionEngine, method, t)?.ReflType).ToArray();
				if (genericArguments.Any(t => t is null))
					return null;

				return genericType.Instantiate(genericArguments);
			}
			case dnlib.DotNet.ElementType.TypedByRef:
				throw new NotImplementedException();
			// TODO: supports typedref
			case dnlib.DotNet.ElementType.I:
				return executionEngine.ResolveType(typeof(nint));
			case dnlib.DotNet.ElementType.U:
				return executionEngine.ResolveType(typeof(nuint));
			case dnlib.DotNet.ElementType.R:
				return executionEngine.ResolveType(typeof(double));
			case dnlib.DotNet.ElementType.FnPtr:
				return executionEngine.ResolveType(typeof(nint));
			case dnlib.DotNet.ElementType.Object:
				return executionEngine.ResolveType(typeof(object));
			case dnlib.DotNet.ElementType.SZArray:
				return TryResolveTypeSigImpl(executionEngine, method, typeSig.Next)?.MakeArrayType();
			case dnlib.DotNet.ElementType.MVar:
				return method?.Instantiation[((GenericMVar)typeSig).Number];
			default:
				throw new NotSupportedException();
			}
		}
	}
}
