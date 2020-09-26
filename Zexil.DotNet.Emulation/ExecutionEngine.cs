using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// CLR context
	/// </summary>
	public sealed unsafe class ExecutionEngineContext : IDisposable {
		internal readonly Dictionary<Assembly, AssemblyDesc> _assemblies = new Dictionary<Assembly, AssemblyDesc>();
		internal readonly Dictionary<Module, ModuleDesc> _modules = new Dictionary<Module, ModuleDesc>();
		internal readonly Dictionary<Type, TypeDesc> _types = new Dictionary<Type, TypeDesc>();
		internal readonly Dictionary<FieldInfo, FieldDesc> _fields = new Dictionary<FieldInfo, FieldDesc>();
		internal readonly Dictionary<MethodBase, MethodDesc> _methods = new Dictionary<MethodBase, MethodDesc>();
		private readonly object _syncRoot = new object();
		private bool _isDisposed;

		/// <summary>
		/// Loaded assemblies by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<AssemblyDesc> Assemblies => _assemblies.Values;

		/// <summary>
		/// Loaded modules by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<ModuleDesc> Modules => _modules.Values;

		/// <summary>
		/// Loaded types by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<TypeDesc> Types => _types.Values;

		/// <summary>
		/// Loaded fields by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<FieldDesc> Fields => _fields.Values;

		/// <summary>
		/// Loaded methods by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<MethodDesc> Methods => _methods.Values;

		/// <summary>
		/// Synchronization root
		/// </summary>
		public object SyncRoot => _syncRoot;

		internal ExecutionEngineContext() {
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				_assemblies.Clear();
				_modules.Clear();
				_types.Clear();
				_fields.Clear();
				_methods.Clear();
				_isDisposed = true;
			}
		}
	}

	/// <summary>
	/// Common Language Runtime implemented by Zexil.DotNet.Emulation
	/// </summary>
	public sealed unsafe class ExecutionEngine : IDisposable {
		private readonly ExecutionEngineContext _context;
		private readonly int _bitness;
		private readonly InterpreterManager _interpreterManager;
		private bool _isDisposed;

		/// <summary>
		/// CLR context
		/// </summary>
		public ExecutionEngineContext Context => _context;

		/// <summary>
		/// CLR bitness
		/// </summary>
		public int Bitness => _bitness;

		/// <summary>
		/// Is 32bit CLR
		/// </summary>
		public bool Is32Bit => _bitness == 32;

		/// <summary>
		/// Is 64bit CLR
		/// </summary>
		public bool Is64Bit => _bitness == 64;

		/// <summary>
		/// Interpreter manager
		/// </summary>
		public InterpreterManager InterpreterManager => _interpreterManager;

		/// <summary>
		/// Constructor
		/// </summary>
		public ExecutionEngine() {
			_context = new ExecutionEngineContext();
			_bitness = sizeof(nint) * 8;
			_interpreterManager = new InterpreterManager(this);
		}

		/// <summary>
		/// Loads an assembly into <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="assemblyData"></param>
		/// <param name="originalAssemblyData"></param>
		/// <returns></returns>
		public AssemblyDesc LoadAssembly(byte[] assemblyData, byte[] originalAssemblyData = null) {
			if (assemblyData is null)
				throw new ArgumentNullException(nameof(assemblyData));

			var assembly = Assembly.Load(assemblyData);
			return LoadAssembly(assembly, originalAssemblyData);
		}

		/// <summary>
		/// Loads an assembly into <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="assemblyPath"></param>
		/// <param name="originalAssemblyData"></param>
		/// <returns></returns>
		public AssemblyDesc LoadAssembly(string assemblyPath, byte[] originalAssemblyData = null) {
			if (string.IsNullOrEmpty(assemblyPath))
				throw new ArgumentNullException(nameof(assemblyPath));

			var assembly = Assembly.LoadFile(Path.GetFullPath(assemblyPath));
			return LoadAssembly(assembly, originalAssemblyData);
		}

		/// <summary>
		/// Loads an assembly into <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="assembly"></param>
		/// <param name="originalAssemblyData"></param>
		/// <returns></returns>
		public AssemblyDesc LoadAssembly(Assembly assembly, byte[] originalAssemblyData = null) {
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			nint rawAssembly = 0;
			if (!(originalAssemblyData is null)) {
				string path = Path.GetTempFileName();
				File.WriteAllBytes(path, originalAssemblyData);
				rawAssembly = Pal.MapFile(path, true);
			}
			var assemblyDesc = new AssemblyDesc(this, assembly, rawAssembly);
			foreach (var module in assembly.Modules)
				ResolveModule(module);
			return assemblyDesc;
		}

		/// <summary>
		/// Resolves an assembly and returns runtime assembly.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public AssemblyDesc ResolveAssembly(Assembly assembly) {
			if (assembly is null)
				throw new ArgumentNullException(nameof(assembly));

			if (_context._assemblies.TryGetValue(assembly, out var assemblyDesc))
				return assemblyDesc;
			throw new ArgumentOutOfRangeException(nameof(assembly), $"{assembly} is not loaded by {nameof(ExecutionEngine)}");
		}

		/// <summary>
		/// Resolves a module and returns runtime module.
		/// </summary>
		/// <param name="module"></param>
		/// <returns></returns>
		public ModuleDesc ResolveModule(Module module) {
			if (module is null)
				throw new ArgumentNullException(nameof(module));

			if (_context._modules.TryGetValue(module, out var moduleDesc))
				return moduleDesc;
			var assemblyDesc = ResolveAssembly(module.Assembly);
			moduleDesc = new ModuleDesc(this, module);
			assemblyDesc._modules.Add(moduleDesc);
			return moduleDesc;
		}

		/// <summary>
		/// Resolves a type and returns runtime type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public TypeDesc ResolveType(Type type) {
			if (type is null)
				throw new ArgumentNullException(nameof(type));

			if (_context._types.TryGetValue(type, out var typeDesc))
				return typeDesc;
			var moduleDesc = ResolveModule(type.Module);
			typeDesc = new TypeDesc(this, type);
			moduleDesc._types.Add(typeDesc);
			return typeDesc;
		}

		/// <summary>
		/// Resolves a field and returns runtime field.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public FieldDesc ResolveField(FieldInfo field) {
			if (field is null)
				throw new ArgumentNullException(nameof(field));

			if (_context._fields.TryGetValue(field, out var fieldDesc))
				return fieldDesc;
			var typeDesc = ResolveType(field.DeclaringType);
			fieldDesc = new FieldDesc(this, field);
			typeDesc._fields.Add(fieldDesc);
			return fieldDesc;
		}

		/// <summary>
		/// Resolves a method and returns runtime method.
		/// </summary>
		/// <param name="method"></param>
		/// <returns></returns>
		public MethodDesc ResolveMethod(MethodBase method) {
			if (method is null)
				throw new ArgumentNullException(nameof(method));

			if (_context._methods.TryGetValue(method, out var methodDesc))
				return methodDesc;
			var typeDesc = ResolveType(method.DeclaringType);
			methodDesc = new MethodDesc(this, method);
			typeDesc._methods.Add(methodDesc);
			return methodDesc;
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var assembly in _context._assemblies.Values) {
					if (assembly.RawAssembly != 0)
						Pal.UnmapFile(assembly.RawAssembly);
				}
				_context.Dispose();
				foreach (var interpreter in _interpreterManager.Interpreters.SelectMany(t => t.Values)) {
					if (interpreter is IDisposable disposable)
						disposable.Dispose();
				}
				_isDisposed = true;
			}
		}

		/// <inheritdoc />
		public override string ToString() {
			return $"ExecutionEngine {_bitness} bit";
		}
	}
}
