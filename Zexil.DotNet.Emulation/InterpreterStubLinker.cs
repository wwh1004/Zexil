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
			private List<Local> _pinnedArguments;
			private Local _returnBuffer;

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
			/// valType  -> ldarga
			/// valType* -> conv_i
			/// genType  -> ldarga (we should dereference in runtime if it is reference type)
			/// genType* -> conv_i
			///
			/// Calling conversation
			/// arguments = method arguments + method return buffer (if method has return value)
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
					_pinnedArguments = new List<Local>();
					_returnBuffer = null;
					EmitPinArguments(parameters);
					// pin arguments
					EmitInitCall(method, parameters);
					// load moduleId, load methodToken
					EmitLoadArguments(parameters);
					// load arguments
					EmitLoadReturnBuffer(method);
					// load return buffer
					EmitLoadTypeArgument(type, typeInstantiation);
					// load typeInstantiation
					EmitLoadMethodArgument(method, methodInstantiation);
					// load methodInstantiation
					EmitInstruction(OpCodes.Call, _dispatch);
					// call InterpreterStub.Dispatch
					EmitUnpinArguments();
					// unpin arguments
					EmitGetReturnValue(method);
					// get return value
					EmitInstruction(OpCodes.Ret);
					// ret
					var locals = new LocalList(_pinnedArguments);
					if (!(_returnBuffer is null))
						locals.Add(_returnBuffer);
					var body = new CilBody(true, _instructions, EmptyExceptionHandlers, locals);

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

			#region emit return buffer
			private void EmitLoadReturnBuffer(MethodDef method) {
				if (!method.HasReturnType)
					return;

				var typeSig = method.ReturnType.RemoveModifiers();
				bool isManaged = !typeSig.IsValueType && !typeSig.IsPointer;
				_returnBuffer = new Local(isManaged ? new PinnedSig(typeSig) : typeSig);
				EmitInstruction(OpCodes.Dup);
				EmitInstruction(OpCodes.Ldc_I4, method.Parameters.Count);
				EmitInstruction(OpCodes.Ldloca, _returnBuffer);
				EmitInstruction(OpCodes.Conv_I);
				EmitInstruction(OpCodes.Stelem_I);
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitLoadReturnBuffer end");
				EmitInstruction(OpCodes.Pop);
#endif
			}

			private void EmitGetReturnValue(MethodDef method) {
				if (!method.HasReturnType)
					return;

				EmitInstruction(OpCodes.Ldloc, _returnBuffer);
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitGetReturnValue end");
				EmitInstruction(OpCodes.Pop);
#endif
			}
			#endregion

			#region emit (un)pin arguments
			private void EmitPinArguments(ParameterList parameters) {
				foreach (var parameter in parameters)
					EmitPinArgument(parameter);
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitPinArguments end");
				EmitInstruction(OpCodes.Pop);
#endif
			}

			private void EmitPinArgument(Parameter parameter) {
				var typeSig = parameter.Type.RemoveModifiers();
				if (typeSig.IsPointer)
					return;
				// type* is no GC type

				if (typeSig.IsByRef) {
					typeSig = typeSig.Next.RemoveModifiers();
					if (typeSig.IsValueType) {
						// valType&
						var local = new Local(new PinnedSig(new ByRefSig(typeSig)));
						// valType& pinned
						_pinnedArguments.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
					}
					else if (typeSig.IsGenericParameter) {
						// T&
						// a really fucking case, we don't know whether it is a value type or not, maybe we should regard it as both value type and reference type
						var local = new Local(new PinnedSig(new ByRefSig(typeSig)));
						// T& pinned
						_pinnedArguments.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
						local = new Local(new PinnedSig(typeSig));
						// T pinned
						_pinnedArguments.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Ldobj, typeSig.ToTypeDefOrRef());
						EmitInstruction(OpCodes.Stloc, local);
					}
					else {
						// refType&
						var local = new Local(new PinnedSig(typeSig));
						// refType pinned
						_pinnedArguments.Add(local);
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
						_pinnedArguments.Add(local);
						EmitInstruction(OpCodes.Ldarg, parameter);
						EmitInstruction(OpCodes.Stloc, local);
					}
				}
			}

			private void EmitUnpinArguments() {
				foreach (var local in _pinnedArguments) {
#if DEBUG
					Debug.Assert(!local.Type.IsValueType);
#endif
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
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitUnpinArguments end");
				EmitInstruction(OpCodes.Pop);
#endif
			}
			#endregion

			#region emit call
			private void EmitInitCall(MethodDef method, ParameterList parameters) {
				EmitInstruction(OpCodes.Ldc_I4, _moduleId);
				EmitInstruction(OpCodes.Ldc_I4, method.MDToken.ToInt32());
				EmitInstruction(OpCodes.Ldc_I4, parameters.Count + (method.HasReturnType ? 1 : 0));
				EmitInstruction(OpCodes.Newarr, new PtrSig(_module.CorLibTypes.Void).ToTypeDefOrRef());
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitInitCall end");
				EmitInstruction(OpCodes.Pop);
#endif
			}

			private void EmitLoadArguments(ParameterList parameters) {
				foreach (var parameter in parameters)
					EmitLoadArgument(parameter);
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitLoadArguments end");
				EmitInstruction(OpCodes.Pop);
#endif
			}

			private void EmitLoadArgument(Parameter parameter) {
				EmitInstruction(OpCodes.Dup);
				EmitInstruction(OpCodes.Ldc_I4, parameter.Index);
				var typeSig = parameter.Type.RemoveModifiers();
				if (typeSig.IsGenericParameter) {
					EmitInstruction(OpCodes.Ldarga, parameter);
					EmitInstruction(OpCodes.Conv_I);
					EmitInstruction(OpCodes.Ldc_I4, 1);
					EmitInstruction(OpCodes.Conv_I);
					EmitInstruction(OpCodes.Or);
				}
				else if (typeSig.IsValueType) {
					EmitInstruction(OpCodes.Ldarga, parameter);
				}
				else {
					EmitInstruction(OpCodes.Ldarg, parameter);
				}
				EmitInstruction(OpCodes.Conv_I);
				EmitInstruction(OpCodes.Stelem_I);
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
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitLoadTypeArgument end");
				EmitInstruction(OpCodes.Pop);
#endif
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
#if DEBUG
				EmitInstruction(OpCodes.Ldstr, "EmitLoadMethodArgument end");
				EmitInstruction(OpCodes.Pop);
#endif
			}
			#endregion

			private void EmitInstruction(OpCode opCode) {
				_instructions.Add(new Instruction(opCode));
			}

			private void EmitInstruction(OpCode opCode, object operand) {
				_instructions.Add(new Instruction(opCode, operand));
			}
		}
	}
}
