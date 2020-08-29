using System;
using System.IO;
using System.Reflection;
using dnlib.DotNet;
using dnlib.DotNet.Writer;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// CLR factory which provides to create an instance of <see cref="ExecutionEngine"/>
	/// </summary>
	public static class ExecutionEngineFactory {
		/// <summary>
		/// Creates an instance of <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <returns></returns>
		public static ExecutionEngine Create(ModuleDef moduleDef) {
			return CreateImpl(moduleDef, null);
		}

		/// <summary>
		/// Creates an instance of <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <param name="originalAssembly"></param>
		/// <returns></returns>
		public static ExecutionEngine Create(ModuleDef moduleDef, byte[] originalAssembly) {
			if (originalAssembly is null)
				throw new ArgumentNullException(nameof(originalAssembly));

			string path = Path.GetTempFileName();
			File.WriteAllBytes(path, originalAssembly);
			return CreateImpl(moduleDef, path);
		}

		/// <summary>
		/// Creates an instance of <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <param name="originalAssemblyPath"></param>
		/// <returns></returns>
		public static ExecutionEngine Create(ModuleDef moduleDef, string originalAssemblyPath) {
			if (originalAssemblyPath is null)
				throw new ArgumentNullException(nameof(originalAssemblyPath));

			return CreateImpl(moduleDef, originalAssemblyPath);
		}

		private static ExecutionEngine CreateImpl(ModuleDef moduleDef, string originalAssemblyPath) {
			if (moduleDef is null)
				throw new ArgumentNullException(nameof(moduleDef));

			byte[] hijackStubData = GenerateInterpreterStub(moduleDef);
			var hijackStub = Assembly.Load(hijackStubData);
			var context = new ExecutionEngineContext(hijackStub, originalAssemblyPath);
			return new ExecutionEngine(context);
		}

		private static byte[] GenerateInterpreterStub(ModuleDef moduleDef) {
			byte[] data;
			using (var stream = new MemoryStream()) {
				var writerOptions = new ModuleWriterOptions(moduleDef);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				//writerOptions.Logger = DummyLogger.NoThrowInstance;
				moduleDef.Write(stream, writerOptions);
				data = stream.ToArray();
			}

			using (var stream = new MemoryStream())
			using (var stubModuleDef = ModuleDefMD.Load(data, new ModuleCreationOptions { TryToLoadPdbFromDisk = false })) {
				GenerateInterpreterStubCore(stubModuleDef);
				var writerOptions = new ModuleWriterOptions(stubModuleDef);
				writerOptions.MetadataOptions.Flags |= MetadataFlags.PreserveAll;
				//writerOptions.Logger = DummyLogger.NoThrowInstance;
				stubModuleDef.Write(stream, writerOptions);
				data = stream.ToArray();
			}

			return data;
		}

		private static void GenerateInterpreterStubCore(ModuleDef moduleDef) {
			/*
			 * TODO:
			 * redirect framework (considering)
			 * to AnyCPU
			 * delete resources
			 */


		}
	}
}
