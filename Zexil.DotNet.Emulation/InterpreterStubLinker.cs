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
				moduleId = new InterpreterStubLinkerCore(stubModule).Link();
				var writerOptions = new ModuleWriterOptions(stubModule);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				//writerOptions.Logger = DummyLogger.NoThrowInstance;
				stubModule.Write(stream, writerOptions);
				data = stream.ToArray();
			}
			return data;
		}

		private sealed class InterpreterStubLinkerCore {
			private static readonly List<ExceptionHandler> EmptyExceptionHandlers = new List<ExceptionHandler>();
			private readonly ModuleDef _module;
			private readonly IMethod _getTypeFromHandle;
			private readonly IMethod _dispatch;
			private readonly int _moduleId;
			private List<Instruction> _instructions;
			private LocalList _locals;

			public InterpreterStubLinkerCore(ModuleDef module) {
				_module = module;
				_getTypeFromHandle = _module.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
				_dispatch = _module.Import(typeof(InterpreterStub).GetMethod("Dispatch"));
				_moduleId = InterpreterStub.AllocateModuleId();
			}

			public int Link() {
				foreach (var method in _module.EnumerateAllMethods()) {
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

					_instructions = new List<Instruction>();
					_locals = new LocalList { new Local(new SZArraySig(_module.CorLibTypes.Object)) };

					EmitInitCall(method, parameters);
					// load moduleId, load methodToken
					for (int i = 0; i < parameters.Count; i++)
						EmitLoadArgument(parameters[i]);
					// load arguments
					if (typeInstantiation.Count > 0)
						EmitLoadTypeArgument(type, typeInstantiation);
					else
						EmitInstruction(OpCodes.Ldnull);
					// load typeInstantiation
					if (methodInstantiation.Count > 0)
						EmitLoadMethodArgument(method, methodInstantiation);
					else
						EmitInstruction(OpCodes.Ldnull);
					// load methodInstantiation
					EmitInstruction(OpCodes.Call, _dispatch);
					// call InterpreterStub.Dispatch
					for (int i = 0; i < parameters.Count; i++)
						EmitSetArgumentIfNeed(parameters[i]);
					// set arguments
					if (method.HasReturnType)
						EmitSetReturnValue(method.ReturnType);
					else
						EmitInstruction(OpCodes.Pop);
					// set return value
					EmitInstruction(OpCodes.Ret);
					// ret
					var body = new CilBody(true, _instructions, EmptyExceptionHandlers, _locals);

					if (!(method.Body is null))
						method.Body = body;
					method.Attributes &= ~MethodAttributes.UnmanagedExport;
					if ((method.ImplAttributes & (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) == (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) {
						method.Body = body;
						method.ImplAttributes &= ~(MethodImplAttributes.Native | MethodImplAttributes.Unmanaged | MethodImplAttributes.PreserveSig);
						method.ImplAttributes |= MethodImplAttributes.IL;
					}
				}

				_module.Resources.Clear();
				if ((_module.Cor20HeaderFlags & ComImageFlags.NativeEntryPoint) == ComImageFlags.NativeEntryPoint)
					_module.NativeEntryPoint = 0;
				_module.Cor20HeaderFlags &= ~(ComImageFlags.ILOnly | ComImageFlags.Bit32Preferred | ComImageFlags.Bit32Required | ComImageFlags.NativeEntryPoint);
				_module.Cor20HeaderFlags |= ComImageFlags.ILOnly;
				if (!(_module.VTableFixups is null))
					_module.VTableFixups.VTables.Clear();

				return _moduleId;
			}

			private void EmitInitCall(MethodDef method, ParameterList parameters) {
				EmitInstruction(OpCodes.Ldc_I4, _moduleId);
				EmitInstruction(OpCodes.Ldc_I4, method.MDToken.ToInt32());
				EmitInstruction(OpCodes.Ldc_I4, parameters.Count);
				EmitInstruction(OpCodes.Newarr, _module.CorLibTypes.Object.TypeDefOrRef);
				EmitInstruction(OpCodes.Dup);
				EmitInstruction(OpCodes.Stloc_0);
			}

			private void EmitLoadArgument(Parameter parameter) {
				EmitInstruction(OpCodes.Dup);
				EmitInstruction(OpCodes.Ldc_I4, parameter.Index);
				EmitInstruction(OpCodes.Ldarg, parameter);
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
						EmitInstruction(ldind);
					}
					EmitInstruction(OpCodes.Box, ((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				}
				else if (typeSig.IsGenericParameter) {
					var typeSpec = new TypeSpecUser(typeSig);
					if (isPointer)
						EmitInstruction(OpCodes.Ldobj, typeSpec);
					EmitInstruction(OpCodes.Box, typeSpec);
				}
				else {
					Debug.Assert((typeSig.ElementType & (ElementType.Class | ElementType.Array | ElementType.SZArray)) != 0);
					if (isPointer)
						EmitInstruction(OpCodes.Ldind_Ref);
				}
				EmitInstruction(OpCodes.Stelem_Ref);
			}

			private void EmitLoadTypeArgument(TypeDef type, IList<GenericParam> typeInstantiation) {
				EmitInstruction(OpCodes.Ldc_I4, typeInstantiation.Count);
				EmitInstruction(OpCodes.Newarr, _module.CorLibTypes.Object.TypeDefOrRef);
				for (int i = 0; i < typeInstantiation.Count; i++) {
					int number = typeInstantiation[i].Number;
					EmitInstruction(OpCodes.Dup);
					EmitInstruction(OpCodes.Ldc_I4, number);
					EmitInstruction(OpCodes.Ldtoken, new TypeSpecUser(new GenericVar(number, type)));
					EmitInstruction(OpCodes.Call, _getTypeFromHandle);
					EmitInstruction(OpCodes.Stelem_Ref);
				}
			}

			private void EmitLoadMethodArgument(MethodDef method, IList<GenericParam> methodInstantiation) {
				EmitInstruction(OpCodes.Ldc_I4, methodInstantiation.Count);
				EmitInstruction(OpCodes.Newarr, _module.CorLibTypes.Object.TypeDefOrRef);
				for (int i = 0; i < methodInstantiation.Count; i++) {
					int number = methodInstantiation[i].Number;
					EmitInstruction(OpCodes.Dup);
					EmitInstruction(OpCodes.Ldc_I4, number);
					EmitInstruction(OpCodes.Ldtoken, new TypeSpecUser(new GenericMVar(number, method)));
					EmitInstruction(OpCodes.Call, _getTypeFromHandle);
					EmitInstruction(OpCodes.Stelem_Ref);
				}
			}

			private void EmitSetArgumentIfNeed(Parameter parameter) {
				var typeSig = parameter.Type.RemovePinnedAndModifiers();
				bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
				if (!isPointer)
					return;
				typeSig = typeSig.Next.RemovePinnedAndModifiers();
				EmitInstruction(OpCodes.Ldarg, parameter);
				EmitInstruction(OpCodes.Ldloc, _locals[0]);
				EmitInstruction(OpCodes.Ldc_I4, parameter.Index);
				EmitInstruction(OpCodes.Ldelem_Ref);
				if (typeSig.IsValueType) {
					EmitInstruction(OpCodes.Unbox_Any, ((TypeDefOrRefSig)typeSig).TypeDefOrRef);
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
					EmitInstruction(stind);
				}
				else if (typeSig.IsGenericParameter) {
					var typeSpec = new TypeSpecUser(typeSig);
					EmitInstruction(OpCodes.Unbox_Any, typeSpec);
					EmitInstruction(OpCodes.Stobj, typeSpec);
				}
				else {
					EmitInstruction(OpCodes.Stind_Ref);
				}
			}

			private void EmitSetReturnValue(TypeSig typeSig) {
				typeSig = typeSig.RemovePinnedAndModifiers();
				bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
				if (isPointer)
					EmitInstruction(OpCodes.Unbox_Any, _module.CorLibTypes.IntPtr.TypeDefOrRef);
				else if (typeSig.IsValueType)
					EmitInstruction(OpCodes.Unbox_Any, ((TypeDefOrRefSig)typeSig).TypeDefOrRef);
				else if (typeSig.IsGenericParameter)
					EmitInstruction(OpCodes.Unbox_Any, new TypeSpecUser(typeSig));
			}

			private void EmitInstruction(OpCode opCode) {
				new Instruction(opCode);
			}

			private void EmitInstruction(OpCode opCode, object operand) {
				new Instruction(opCode, operand);
			}
		}
	}
}
