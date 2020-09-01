using System;
using System.Collections.Generic;
using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime assembly
	/// </summary>
	public sealed unsafe class AssemblyDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly Assembly _reflAssembly;
		private readonly void* _rawAssembly;
		internal readonly List<ModuleDesc> _modules;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Assembly
		/// </summary>
		public Assembly ReflAssembly => _reflAssembly;

		/// <summary>
		/// Original assembly data
		/// </summary>
		public void* RawAssembly => _rawAssembly;

		/// <summary>
		/// Loaded modules
		/// </summary>
		public IEnumerable<ModuleDesc> Modules => _modules;

		internal AssemblyDesc(ExecutionEngine executionEngine, Assembly reflAssembly, void* rawAssembly) {
			_executionEngine = executionEngine;
			_reflAssembly = reflAssembly;
			_rawAssembly = rawAssembly;
			_modules = new List<ModuleDesc>();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _reflAssembly.ToString();
		}
	}

	/// <summary>
	/// Runtime module
	/// </summary>
	public sealed class ModuleDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly Module _reflModule;
		internal readonly List<TypeDesc> _types;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Module
		/// </summary>
		public Module ReflModule => _reflModule;

		/// <summary>
		/// Loaded types
		/// </summary>
		public IEnumerable<TypeDesc> Types => _types;

		/// <summary>
		/// Resolves a type and return runtime type
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public TypeDesc ResolveType(int metadataToken) {
			var type = _reflModule.ResolveType(metadataToken);
			return _executionEngine.ResolveType(type);
		}

		/// <summary>
		/// Resolves a field and return runtime field
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public FieldDesc ResolveField(int metadataToken) {
			var field = _reflModule.ResolveField(metadataToken);
			return _executionEngine.ResolveField(field);
		}

		/// <summary>
		/// Resolves a method and return runtime method
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public MethodDesc ResolveMethod(int metadataToken) {
			var method = _reflModule.ResolveMethod(metadataToken);
			return _executionEngine.ResolveMethod(method);
		}

		internal ModuleDesc(ExecutionEngine executionEngine, Module reflModule) {
			_executionEngine = executionEngine;
			_reflModule = reflModule;
			_types = new List<TypeDesc>();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _reflModule.ToString();
		}
	}

	/// <summary>
	/// Runtime type
	/// </summary>
	public sealed class TypeDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly Type _reflType;
		internal readonly List<FieldDesc> _fields;
		internal readonly List<MethodDesc> _methods;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Reflection type
		/// </summary>
		public Type ReflType => _reflType;

		/// <summary>
		/// Loaded fields
		/// </summary>
		public IEnumerable<FieldDesc> Fields => _fields;

		/// <summary>
		/// Loaded methods
		/// </summary>
		public IEnumerable<MethodDesc> Methods => _methods;

		internal TypeDesc(ExecutionEngine executionEngine, Type reflType) {
			_executionEngine = executionEngine;
			_reflType = reflType;
			_fields = new List<FieldDesc>();
			_methods = new List<MethodDesc>();
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _reflType.ToString();
		}
	}

	/// <summary>
	/// Runtime field
	/// </summary>
	public sealed unsafe class FieldDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly FieldInfo _reflField;
		private readonly TypeDesc _declaringType;
		private readonly uint _offset;
		private readonly bool _isThreadStatic;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Reflection field
		/// </summary>
		public FieldInfo ReflField => _reflField;

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType => _declaringType;

		/// <summary>
		/// Field offset
		/// </summary>
		public uint Offset => _offset;

		/// <summary>
		/// Is a static field
		/// </summary>
		public bool IsStatic => _offset == uint.MaxValue;

		/// <summary>
		/// Has <see cref="ThreadStaticAttribute"/>
		/// </summary>
		public bool IsThreadStatic => _isThreadStatic;

		internal FieldDesc(ExecutionEngine executionEngine, FieldInfo reflField) {
			_executionEngine = executionEngine;
			_reflField = reflField;
			_declaringType = executionEngine.ResolveType(reflField.FieldType);
			_offset = !reflField.IsStatic ? Unsafe.GetFieldOffset((void*)reflField.FieldHandle.Value) : uint.MaxValue;
			_isThreadStatic = !(reflField.GetCustomAttribute<ThreadStaticAttribute>() is null);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _reflField.ToString();
		}
	}

	/// <summary>
	/// Runtime method
	/// </summary>
	public sealed class MethodDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly MethodBase _reflMethod;
		private readonly TypeDesc _declaringType;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Reflection method
		/// </summary>
		public MethodBase ReflMethod => _reflMethod;

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType => _declaringType;

		internal MethodDesc(ExecutionEngine executionEngine, MethodBase reflMethod) {
			_executionEngine = executionEngine;
			_reflMethod = reflMethod;
			_declaringType = executionEngine.ResolveType(reflMethod.DeclaringType);
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _reflMethod.ToString();
		}
	}
}
