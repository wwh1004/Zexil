using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Exception thrown by <see cref="ExecutionEngine"/>
	/// </summary>
	public class ExecutionEngineException : Exception {
		/// <inheritdoc/>
		public ExecutionEngineException() {
		}

		/// <inheritdoc/>
		public ExecutionEngineException(string message) : base(message) {
		}

		/// <inheritdoc/>
		public ExecutionEngineException(Exception inner) : base(inner.Message, inner) {
		}

		/// <inheritdoc/>
		public ExecutionEngineException(string message, Exception inner) : base(message, inner) {
		}
	}

	/// <summary>
	/// CLR context
	/// </summary>
	public sealed unsafe class ExecutionEngineContext : IDisposable {
		/// <summary>
		/// Default stack size
		/// </summary>
		public const uint DefaultStackSize = 0x800000;

		private readonly Dictionary<Assembly, AssemblyDesc> _assemblies = new Dictionary<Assembly, AssemblyDesc>();
		private readonly Dictionary<Type, TypeDesc> _types = new Dictionary<Type, TypeDesc>();
		private readonly List<IntPtr> _stackBases = new List<IntPtr>();
		private readonly object _syncRoot = new object();
		[ThreadStatic]
		private byte* _stackBase;
		// 8MB stack for each thread
		[ThreadStatic]
		private byte* _stack;
		private bool _isDisposed;

		internal Dictionary<Assembly, AssemblyDesc> InternalAssemblies => _assemblies;

		internal Dictionary<Type, TypeDesc> InternalTypes => _types;

		/// <summary>
		/// Loaded assemblies by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<AssemblyDesc> Assemblies => _assemblies.Values;

		/// <summary>
		/// Loaded types by <see cref="ExecutionEngine"/>
		/// </summary>
		public IEnumerable<TypeDesc> Types => _types.Values;

		/// <summary>
		/// Stack base pointer (default is 8MB size for each thread)
		/// </summary>
		public byte* StackBase {
			get {
				if (_stackBase == null) {
					_stackBase = (byte*)Pal.AllocMemory(DefaultStackSize, false);
					lock (((ICollection)_stackBases).SyncRoot)
						_stackBases.Add((IntPtr)_stackBase);
				}
				return _stackBase;
			}
		}

		/// <summary>
		/// Stack pointer (default is 8MB size for each thread)
		/// </summary>
		public byte* Stack {
			get {
				if (_stack == null)
					_stack = StackBase;
				return _stack;
			}
			set {
				byte* stackBase = StackBase;
				if (value < stackBase || value > stackBase + DefaultStackSize)
					throw new ExecutionEngineException(new ArgumentOutOfRangeException());

				_stack = value;
			}
		}

		/// <summary>
		/// Synchronization root
		/// </summary>
		public object SyncRoot => _syncRoot;

		/// <inheritdoc/>
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var assembly in _assemblies.Values)
					Pal.UnmapFile(assembly.RawAssembly);
				_assemblies.Clear();
				_types.Clear();
				foreach (var stackBase in _stackBases)
					Pal.FreeMemory((void*)stackBase);
				_stackBases.Clear();
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
		private readonly Interpreter _interpreter;
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
		/// Interpreter
		/// </summary>
		public Interpreter Interpreter => _interpreter;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context"></param>
		public ExecutionEngine(ExecutionEngineContext context) {
			_context = context;
			_bitness = sizeof(void*) * 8;
		}

		/// <summary>
		/// Loads an assembly into <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="assemblyData"></param>
		/// <param name="originalAssemblyData"></param>
		/// <returns></returns>
		public AssemblyDesc LoadAssembly(byte[] assemblyData, byte[] originalAssemblyData) {
			if (assemblyData is null)
				throw new ArgumentNullException(nameof(assemblyData));
			if (originalAssemblyData is null)
				throw new ArgumentNullException(nameof(originalAssemblyData));

			try {
				string path = Path.GetTempFileName();
				File.WriteAllBytes(path, originalAssemblyData);
				var assembly = Assembly.Load(assemblyData);
				var assemblyDesc = new AssemblyDesc(assembly, Pal.MapFile(path, true));
				_context.InternalAssemblies.Add(assembly, assemblyDesc);
				return assemblyDesc;
			}
			catch (Exception ex) {
				throw new ExecutionEngineException(ex);
			}
		}

		/// <summary>
		/// Resolves an assembly and returns runtime assembly.
		/// </summary>
		/// <param name="assembly"></param>
		/// <returns></returns>
		public AssemblyDesc ResolveAssembly(Assembly assembly) {
			if (assembly is null)
				throw new ExecutionEngineException(new ArgumentNullException(nameof(assembly)));

			if (_context.InternalAssemblies.TryGetValue(assembly, out var assemblyDesc))
				return assemblyDesc;
			throw new ExecutionEngineException(new ArgumentOutOfRangeException(nameof(assembly), $"{assembly} is not loaded by {nameof(ExecutionEngine)}"));
		}

		/// <summary>
		/// Resolves a type and returns runtime type.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public TypeDesc ResolveType(Type type) {
			if (type is null)
				throw new ExecutionEngineException(new ArgumentNullException(nameof(type)));

			if (_context.InternalTypes.TryGetValue(type, out var typeDesc))
				return typeDesc;
			ResolveAssembly(type.Assembly);
			// Checks whether assembly that contains this type is loaded by EE
			return new TypeDesc(this, type);
		}

		internal void AddType(TypeDesc type) {
			_context.InternalTypes.Add(type.InternalValue, type);
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!_isDisposed) {
				_context.Dispose();
				_isDisposed = true;
			}
		}

		/// <inheritdoc/>
		public override string ToString() {
			return $"ExecutionEngine {_bitness} bit";
		}
	}
}
