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
			private readonly IType _type;
			private readonly IMethod _getTypeFromHandle;
			private readonly IMethod _dispatch;
			private readonly int _moduleId;
			private List<Instruction> _instructions;
			private LocalList _locals;

			public InterpreterStubLinkerCore(ModuleDef module) {
				_module = module;
				_type = _module.CorLibTypes.GetTypeRef("System", "Type");
				_getTypeFromHandle = _module.Import(typeof(Type).GetMethod("GetTypeFromHandle"));
				_dispatch = _module.Import(typeof(InterpreterStub).GetMethod("Dispatch"));
				_moduleId = InterpreterStub.AllocateModuleId();
			}

			/// <summary>
			/// Type conversation (arguments and return value are the same):
			/// refType  -> no conv
			/// refType* -> conv_i
			/// valType  -> box
			/// valType* -> conv_i
			/// genType  -> box
			/// genType* -> conv_i
			/// </summary>
			/// <returns></returns>
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
					_locals = new LocalList();
					EmitPinArgumentsIfNeed(parameters);
					// pin arguments if need
					EmitInitCall(method, parameters);
					// load moduleId, load methodToken
					EmitLoadArguments(parameters);
					// load arguments
					EmitLoadTypeArgument(type, typeInstantiation);
					// load typeInstantiation
					EmitLoadMethodArgument(method, methodInstantiation);
					// load methodInstantiation
					EmitInstruction(OpCodes.Call, _dispatch);
					// call InterpreterStub.Dispatch
					EmitSetReturnValue(method);
					// set return value
					EmitUnpinArgumentsIfNeed();
					// unpin arguments if need
					EmitInstruction(OpCodes.Ret);
					// ret
					var body = new CilBody(false, _instructions, EmptyExceptionHandlers, _locals);

					if (!(method.Body is null)) {
						method.FreeMethodBody();
						method.Body = body;
					}
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

			private void EmitPinArgumentsIfNeed(ParameterList parameters) {
				foreach (var parameter in parameters)
					EmitPinArgumentIfNeed(parameter);
			}

			private void EmitPinArgumentIfNeed(Parameter parameter) {
				var typeSig = parameter.Type.RemoveModifiers();
				bool isUnmanagedPointer = typeSig.IsPointer;
				bool isManagedPointer = typeSig.IsByRef;
				if (isUnmanagedPointer)
					return;
				// type* is no GC type

				if (isManagedPointer) {
					typeSig = typeSig.Next.RemoveModifiers();
					if (typeSig.IsValueType) {
						// valType&
						var local = new Local(new PinnedSig(new ByRefSig(typeSig)));
						// valType& pinned 
						_locals.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
					}
					else if (typeSig.IsGenericParameter) {
						// T&
						// a really fucking case, we don't know whether it is a value type or not, maybe we should regard it as both value type and reference type
						var local = new Local(new PinnedSig(new ByRefSig(typeSig)));
						// T& pinned 
						_locals.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
						local = new Local(new PinnedSig(typeSig));
						// T pinned
						_locals.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Ldobj, typeSig.ToTypeDefOrRef());
						EmitInstruction(OpCodes.Stloc, local);
					}
					else {
						// refType&
						var local = new Local(new PinnedSig(typeSig));
						// refType pinned
						_locals.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Ldobj, typeSig.ToTypeDefOrRef());
						EmitInstruction(OpCodes.Stloc, local);
					}
				}
				else {
					if (typeSig.IsValueType) {
						// valType
						return;
					}
					else {
						// T/refType
						// for T, it is also a fucking case, we don't know whether it is a value type or not, maybe we should regard it as reference type
						var local = new Local(new PinnedSig(typeSig));
						// T/refType pinned
						_locals.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
					}
				}
			}

			private void EmitInitCall(MethodDef method, ParameterList parameters) {
				EmitInstruction(OpCodes.Ldc_I4, _moduleId);
				EmitInstruction(OpCodes.Ldc_I4, method.MDToken.ToInt32());
				EmitInstruction(OpCodes.Ldc_I4, parameters.Count);
				EmitInstruction(OpCodes.Newarr, _module.CorLibTypes.Object.TypeDefOrRef);
			}

			private void EmitLoadArguments(ParameterList parameters) {
				foreach (var parameter in parameters)
					EmitLoadArgument(parameter);
			}

			private void EmitLoadArgument(Parameter parameter) {
				EmitInstruction(OpCodes.Dup);
				EmitInstruction(OpCodes.Ldc_I4, parameter.Index);
				EmitInstruction(OpCodes.Ldarg, parameter);
				var typeSig = parameter.Type.RemoveModifiers();
				bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
				if (isPointer)
					typeSig = typeSig.Next.RemoveModifiers();
				Debug.Assert(typeSig is TypeDefOrRefSig || typeSig is GenericSig || typeSig is GenericInstSig);
				if (isPointer) {
					EmitInstruction(OpCodes.Conv_I);
					EmitInstruction(OpCodes.Box, _module.CorLibTypes.IntPtr.TypeDefOrRef);
				}
				else if (typeSig.IsValueType || typeSig.IsGenericParameter) {
					EmitInstruction(OpCodes.Box, typeSig.ToTypeDefOrRef());
				}
				EmitInstruction(OpCodes.Stelem_Ref);
			}

			private void EmitLoadTypeArgument(TypeDef type, IList<GenericParam> typeInstantiation) {
				if (typeInstantiation.Count == 0) {
					EmitInstruction(OpCodes.Ldnull);
					return;
				}

				EmitInstruction(OpCodes.Ldc_I4, typeInstantiation.Count);
				EmitInstruction(OpCodes.Newarr, _type);
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
				if (methodInstantiation.Count == 0) {
					EmitInstruction(OpCodes.Ldnull);
					return;
				}

				EmitInstruction(OpCodes.Ldc_I4, methodInstantiation.Count);
				EmitInstruction(OpCodes.Newarr, _type);
				for (int i = 0; i < methodInstantiation.Count; i++) {
					int number = methodInstantiation[i].Number;
					EmitInstruction(OpCodes.Dup);
					EmitInstruction(OpCodes.Ldc_I4, number);
					EmitInstruction(OpCodes.Ldtoken, new TypeSpecUser(new GenericMVar(number, method)));
					EmitInstruction(OpCodes.Call, _getTypeFromHandle);
					EmitInstruction(OpCodes.Stelem_Ref);
				}
			}

			private void EmitSetReturnValue(MethodDef method) {
				if (!method.HasReturnType) {
					EmitInstruction(OpCodes.Pop);
					return;
				}

				var typeSig = method.ReturnType.RemoveModifiers();
				bool isPointer = typeSig.IsByRef || typeSig.IsPointer;
				if (isPointer)
					EmitInstruction(OpCodes.Unbox_Any, _module.CorLibTypes.IntPtr.TypeDefOrRef);
				else if (typeSig.IsValueType || typeSig.IsGenericParameter)
					EmitInstruction(OpCodes.Unbox_Any, typeSig.ToTypeDefOrRef());
				else
					Debug.Assert(typeSig is TypeDefOrRefSig);
			}

			private void EmitUnpinArgumentsIfNeed() {
				foreach (var local in _locals) {
					Debug.Assert(local.Type is PinnedSig);
					var typeSig = local.Type.Next;
					if (typeSig.IsGenericParameter) {
						EmitInstruction(OpCodes.Ldloca, local);
						EmitInstruction(OpCodes.Initobj, typeSig.ToTypeDefOrRef());
					}
					else {
						EmitInstruction(OpCodes.Ldnull);
						EmitInstruction(OpCodes.Stloc, local);
					}
				}
			}

			private void EmitInstruction(OpCode opCode) {
				_instructions.Add(new Instruction(opCode));
			}

			private void EmitInstruction(OpCode opCode, object operand) {
				_instructions.Add(new Instruction(opCode, operand));
			}
		}
	}
}
