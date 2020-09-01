using System.Collections.Generic;
using System.IO;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using dnlib.DotNet.MD;
using dnlib.DotNet.Writer;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// [WIP] Interpreter stub linker TODO:
	/// </summary>
	public static class InterpreterStubLinker {
		/// <summary>
		/// Links AnyCPU interpreter stub for <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public static byte[] Link(ModuleDef module) {
			using (var stream = new MemoryStream()) {
				var writerOptions = new ModuleWriterOptions(module);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll | MetadataFlags.KeepOldMaxStack;
				writerOptions.Logger = DummyLogger.NoThrowInstance;
				module.Write(stream, writerOptions);
				return Link(stream.ToArray());
			}
		}

		/// <summary>
		/// Links AnyCPU interpreter stub for <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
		public static byte[] Link(byte[] data) {
			using (var stream = new MemoryStream())
			using (var stubModule = ModuleDefMD.Load(data, new ModuleCreationOptions { TryToLoadPdbFromDisk = false })) {
				LinkInterpreterStubCore(stubModule);
				var writerOptions = new ModuleWriterOptions(stubModule);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				//writerOptions.Logger = DummyLogger.NoThrowInstance;
				stubModule.Write(stream, writerOptions);
				data = stream.ToArray();
			}
			return data;
		}

		private static void LinkInterpreterStubCore(ModuleDef module) {
			// TODO: redirect to interpreter stub. redirect framework (considering)
			var emptyBody = new CilBody(false, new List<Instruction> { OpCodes.Ret.ToInstruction() }, new List<ExceptionHandler>(), new List<Local>());

			foreach (var method in module.EnumerateAllMethods()) {
				//foreach (var parameter in method.Parameters)
				//	parameter.Name = string.Empty;
				//foreach (var genericParameter in method.GenericParameters)
				//	genericParameter.Name = UTF8String.Empty;			
				if (!(method.Body is null))
					method.Body = emptyBody;
				method.Attributes &= ~MethodAttributes.UnmanagedExport;
				if ((method.ImplAttributes & (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) == (MethodImplAttributes.Native | MethodImplAttributes.Unmanaged)) {
					method.Body = emptyBody;
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
		}
	}
}
