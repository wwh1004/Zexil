using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter stub linker
	/// </summary>
	public static class InterpreterStubLinker {
		/// <summary>
		/// Links AnyCPU interpreter stub for <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="module"></param>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		public static byte[] Link(ModuleDef module, out int moduleId) {
			using (var stream = new MemoryStream()) {
				var writerOptions = new ModuleWriterOptions(module);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
				writerOptions.Logger = DummyLogger.NoThrowInstance;
				module.Write(stream, writerOptions);
				return Link(stream.ToArray(), out moduleId);
			}
		}

		/// <summary>
		/// Links AnyCPU interpreter stub for <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="data"></param>
		/// <param name="moduleId"></param>
		/// <returns></returns>
		public static byte[] Link(byte[] data, out int moduleId) {
			using (var stream = new MemoryStream())
			using (var stubModule = ModuleDefMD.Load(data, new ModuleCreationOptions { TryToLoadPdbFromDisk = false })) {
				moduleId = LinkInterpreterStubCore(stubModule);
				var writerOptions = new ModuleWriterOptions(stubModule);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				//writerOptions.Logger = DummyLogger.NoThrowInstance;
				stubModule.Write(stream, writerOptions);
				data = stream.ToArray();
			}
			return data;
		}

		private static int LinkInterpreterStubCore(ModuleDef module) {
			var cachedInstructionLists = new Dictionary<int, List<Instruction>>();
			var emptyExceptionHandlers = new List<ExceptionHandler>();
			var emptyLocals = new LocalList();
			var argsStoredLocals = new LocalList { new Local(new SZArraySig(module.CorLibTypes.Object)) };
			int moduleId = InterpreterStub.AllocateModuleId();
			var getTypeFromHandle = module.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
			var dispatch = module.Import(typeof(InterpreterStub).GetMethod("Dispatch"));

			foreach (var method in module.EnumerateAllMethods()) {
				var parameters = method.Parameters;
				if (parameters.Count > ushort.MaxValue)
					throw new NotSupportedException();
				var methodInstantiation = method.GenericParameters;
				if (methodInstantiation.Count > byte.MaxValue)
					throw new NotSupportedException();
				var type = method.DeclaringType;
				var typeInstantiation = type.GenericParameters;
				if (typeInstantiation.Count > byte.MaxValue)
					throw new NotSupportedException();

#if DEBUG
				for (int i = 0; i < typeInstantiation.Count; i++)
					Debug.Assert(typeInstantiation[i].Number == i);
				for (int i = 0; i < methodInstantiation.Count; i++)
					Debug.Assert(methodInstantiation[i].Number == i);
#endif

				int cacheKey = parameters.Count | (typeInstantiation.Count << 16) | (methodInstantiation.Count << 24);
				foreach (var parameter in parameters) {
					if (!parameter.Type.RemovePinnedAndModifiers().IsByRef)
						continue;
					cacheKey = -1;
					break;
				}
				if (cacheKey != -1 && method.ReturnType.RemovePinnedAndModifiers().IsByRef)
					cacheKey = -1;
				if (cacheKey == -1 || !cachedInstructionLists.TryGetValue(cacheKey, out var instructions)) {
					instructions = new List<Instruction> {
						new Instruction(OpCodes.Ldc_I4, moduleId),
						new Instruction(OpCodes.Ldc_I4, method.MDToken.ToInt32()),
						new Instruction(OpCodes.Ldc_I4, parameters.Count),
						new Instruction(OpCodes.Newarr, module.CorLibTypes.Object.TypeDefOrRef)
					};
					// load moduleId, load methodToken
					if (cacheKey == -1) {
						instructions.Add(new Instruction(OpCodes.Dup));
						instructions.Add(new Instruction(OpCodes.Stloc_0));
					}
					// store argument array if need
					for (int i = 0; i < parameters.Count; i++)
						EmitLoadArgument(instructions, parameters[i]);
					// load arguments
					if (typeInstantiation.Count > 0)
						EmitLoadTypeArgument(instructions, type, typeInstantiation, module.CorLibTypes.Object.TypeDefOrRef, getTypeFromHandle);
					else
						instructions.Add(new Instruction(OpCodes.Ldnull));
					// load typeInstantiation
					if (methodInstantiation.Count > 0)
						EmitLoadMethodArgument(instructions, method, methodInstantiation, module.CorLibTypes.Object.TypeDefOrRef, getTypeFromHandle);
					else
						instructions.Add(new Instruction(OpCodes.Ldnull));
					// load methodInstantiation
					instructions.Add(new Instruction(OpCodes.Call, dispatch));
					// call InterpreterStub.Dispatch
					for (int i = 0; i < parameters.Count; i++)
						EmitSetArguments(instructions, parameters[i], module.CorLibTypes.IntPtr.TypeDefOrRef);
					// set arguments
					if (method.HasReturnType)
						EmitSetReturnValue(instructions, method.ReturnType, module.CorLibTypes.IntPtr.TypeDefOrRef);
					else
						instructions.Add(new Instruction(OpCodes.Pop));
					// set return value
					instructions.Add(new Instruction(OpCodes.Ret));
					// ret
					if (cacheKey != -1)
						cachedInstructionLists.Add(cacheKey, instructions);
				}
				else {
					instructions = new List<Instruction>(instructions) { [1] = new Instruction(OpCodes.Ldc_I4, method.MDToken.ToInt32()) };
					// patch methodToken
				}
				var body = new CilBody(cacheKey == -1, instructions, emptyExceptionHandlers, cacheKey != -1 ? emptyLocals : argsStoredLocals);

				if (!(method.Body is null))
					method.Body = body;
				method.Attributes &= ~MethodAttributes.UnmanagedExport;
				if ((method.ImplAttributes & (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) == (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) {
					method.Body = body;
					method.ImplAttributes &= ~(MethodImplAttributes.Native | MethodImplAttributes.Unmanaged | MethodImplAttributes.PreserveSig);
					method.ImplAttributes |= MethodImplAttributes.IL;
				}
			}

			module.Resources.Clear();
			if ((module.Cor20HeaderFlags & ComImageFlags.NativeEntryPoint) == ComImageFlags.NativeEntryPoint)
				module.NativeEntryPoint = 0;
			module.Cor20HeaderFlags &= ~(ComImageFlags.ILOnly | ComImageFlags.Bit32Preferred | ComImageFlags.Bit32Required | ComImageFlags.NativeEntryPoint);
			module.Cor20HeaderFlags |= ComImageFlags.ILOnly;
			if (!(module.VTableFixups is null))
				module.VTableFixups.VTables.Clear();

			return moduleId;
		}

		private static void EmitLoadArgument(List<Instruction> instructions, Parameter parameter) {
			instructions.Add(new Instruction(OpCodes.Dup));
			instructions.Add(new Instruction(OpCodes.Ldc_I4, parameter.Index));
			instructions.Add(new Instruction(OpCodes.Ldarg, parameter));
			var typeSig = parameter.Type.RemovePinnedAndModifiers();
			bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
			if (isPointer)
				typeSig = typeSig.Next.RemovePinnedAndModifiers();
			Debug.Assert(typeSig is TypeDefOrRefSig || typeSig is GenericSig || typeSig is GenericInstSig);
			if (typeSig.IsValueType) {
				if (isPointer) {
					OpCode ldind;
					switch (typeSig.ElementType) {
					case ElementType.I1: ldind = OpCodes.Ldind_I1; break;
					case ElementType.U1: case ElementType.Boolean: ldind = OpCodes.Ldind_U1; break;
					case ElementType.I2: ldind = OpCodes.Ldind_I2; break;
					case ElementType.U2: case ElementType.Char: ldind = OpCodes.Ldind_U2; break;
					case ElementType.I4: ldind = OpCodes.Ldind_I4; break;
					case ElementType.U4: ldind = OpCodes.Ldind_U4; break;
					case ElementType.I8: case ElementType.U8: ldind = OpCodes.Ldind_I8; break;
					case ElementType.R4: ldind = OpCodes.Ldind_R4; break;
					case ElementType.R8: ldind = OpCodes.Ldind_R8; break;
					case ElementType.I: case ElementType.U: ldind = OpCodes.Ldind_I; break;
					default: throw new NotImplementedException($"EmitBox unhandled case: {{IsValueType=true, ElementType={typeSig.ElementType}}}");
					}
					instructions.Add(new Instruction(ldind));
				}
				instructions.Add(new Instruction(OpCodes.Box, ((TypeDefOrRefSig)typeSig).TypeDefOrRef));
			}
			else if (typeSig.IsGenericParameter) {
				var typeSpec = new TypeSpecUser(typeSig);
				if (isPointer)
					instructions.Add(new Instruction(OpCodes.Ldobj, typeSpec));
				instructions.Add(new Instruction(OpCodes.Box, typeSpec));
			}
			else {
				Debug.Assert((typeSig.ElementType & (ElementType.Class | ElementType.Array | ElementType.SZArray)) != 0);
				if (isPointer)
					instructions.Add(new Instruction(OpCodes.Ldind_Ref));
			}
			instructions.Add(new Instruction(OpCodes.Stelem_Ref));
		}

		private static void EmitLoadTypeArgument(List<Instruction> instructions, TypeDef type, IList<GenericParam> typeInstantiation, ITypeDefOrRef @object, IMethod getTypeFromHandle) {
			instructions.Add(new Instruction(OpCodes.Ldc_I4, typeInstantiation.Count));
			instructions.Add(new Instruction(OpCodes.Newarr, @object));
			for (int i = 0; i < typeInstantiation.Count; i++) {
				int number = typeInstantiation[i].Number;
				instructions.Add(new Instruction(OpCodes.Dup));
				instructions.Add(new Instruction(OpCodes.Ldc_I4, number));
				instructions.Add(new Instruction(OpCodes.Ldtoken, new TypeSpecUser(new GenericVar(number, type))));
				instructions.Add(new Instruction(OpCodes.Call, getTypeFromHandle));
				instructions.Add(new Instruction(OpCodes.Stelem_Ref));
			}
		}

		private static void EmitLoadMethodArgument(List<Instruction> instructions, MethodDef method, IList<GenericParam> methodInstantiation, ITypeDefOrRef @object, IMethod getTypeFromHandle) {
			instructions.Add(new Instruction(OpCodes.Ldc_I4, methodInstantiation.Count));
			instructions.Add(new Instruction(OpCodes.Newarr, @object));
			for (int i = 0; i < methodInstantiation.Count; i++) {
				int number = methodInstantiation[i].Number;
				instructions.Add(new Instruction(OpCodes.Dup));
				instructions.Add(new Instruction(OpCodes.Ldc_I4, number));
				instructions.Add(new Instruction(OpCodes.Ldtoken, new TypeSpecUser(new GenericMVar(number, method))));
				instructions.Add(new Instruction(OpCodes.Call, getTypeFromHandle));
				instructions.Add(new Instruction(OpCodes.Stelem_Ref));
			}
		}

		private static void EmitSetArguments(List<Instruction> instructions, Parameter parameter, ITypeDefOrRef nativeInt) {
			var typeSig = parameter.Type.RemovePinnedAndModifiers();
			bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
			if (!isPointer)
				return;
			typeSig = typeSig.Next.RemovePinnedAndModifiers();
			instructions.Add(new Instruction(OpCodes.Ldarg, parameter));
			instructions.Add(new Instruction(OpCodes.Ldloc_0));
			instructions.Add(new Instruction(OpCodes.Ldc_I4, parameter.Index));
			instructions.Add(new Instruction(OpCodes.Ldelem_Ref));
			if (typeSig.IsValueType) {
				instructions.Add(new Instruction(OpCodes.Unbox_Any, ((TypeDefOrRefSig)typeSig).TypeDefOrRef));
				OpCode stind;
				switch (typeSig.ElementType) {
				case ElementType.I1: case ElementType.U1: case ElementType.Boolean: stind = OpCodes.Stind_I1; break;
				case ElementType.I2: case ElementType.U2: case ElementType.Char: stind = OpCodes.Stind_I2; break;
				case ElementType.I4: case ElementType.U4: stind = OpCodes.Stind_I4; break;
				case ElementType.I8: case ElementType.U8: stind = OpCodes.Stind_I8; break;
				case ElementType.R4: stind = OpCodes.Stind_R4; break;
				case ElementType.R8: stind = OpCodes.Stind_R8; break;
				case ElementType.I: case ElementType.U: stind = OpCodes.Stind_I; break;
				default: throw new NotImplementedException($"EmitUnbox unhandled case: {{IsValueType=true, ElementType={typeSig.ElementType}}}");
				}
				instructions.Add(new Instruction(stind));
			}
			else if (typeSig.IsGenericParameter) {
				var typeSpec = new TypeSpecUser(typeSig);
				instructions.Add(new Instruction(OpCodes.Unbox_Any, typeSpec));
				instructions.Add(new Instruction(OpCodes.Stobj, typeSpec));
			}
			else {
				instructions.Add(new Instruction(OpCodes.Stind_Ref));
			}
		}

		private static void EmitSetReturnValue(List<Instruction> instructions, TypeSig typeSig, ITypeDefOrRef nativeInt) {
			typeSig = typeSig.RemovePinnedAndModifiers();
			bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
			if (isPointer)
				instructions.Add(new Instruction(OpCodes.Unbox_Any, nativeInt));
			else if (typeSig.IsValueType)
				instructions.Add(new Instruction(OpCodes.Unbox_Any, ((TypeDefOrRefSig)typeSig).TypeDefOrRef));
			else if (typeSig.IsGenericParameter)
				instructions.Add(new Instruction(OpCodes.Unbox_Any, new TypeSpecUser(typeSig)));
		}
	}
}
