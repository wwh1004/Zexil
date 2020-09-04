using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime assembly
	/// </summary>
	public sealed unsafe class AssemblyDesc {
		private readonly ExecutionEngine _executionEngine;
		private readonly Assembly _reflAssembly;
		private readonly string _fullName;
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
		/// Assembly full name
		/// </summary>
		public string FullName => _fullName;

		/// <summary>
		/// Original assembly data
		/// </summary>
		public void* RawAssembly => _rawAssembly;

		/// <summary>
		/// Loaded modules
		/// </summary>
		public IEnumerable<ModuleDesc> Modules => _modules;

		/// <summary>
		/// Manifest module (the first module in <see cref="Modules"/>)
		/// </summary>
		public ModuleDesc ManifestModule => _modules[0];

		internal AssemblyDesc(ExecutionEngine executionEngine, Assembly reflAssembly, void* rawAssembly) {
			executionEngine.Context._assemblies.Add(reflAssembly, this);
			_executionEngine = executionEngine;
			_reflAssembly = reflAssembly;
			_fullName = reflAssembly.FullName;
			_rawAssembly = rawAssembly;
			_modules = new List<ModuleDesc>();
		}

		/// <inheritdoc />
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
		private readonly AssemblyDesc _assembly;
		private readonly string _scopeName;
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
		/// Declaring assembly
		/// </summary>
		public AssemblyDesc Assembly => _assembly;

		/// <summary>
		/// Scope name
		/// </summary>
		public string ScopeName => _scopeName;

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
			executionEngine.Context._modules.Add(reflModule, this);
			_executionEngine = executionEngine;
			_reflModule = reflModule;
			_assembly = executionEngine.ResolveAssembly(reflModule.Assembly);
			_scopeName = reflModule.ScopeName;
			_types = new List<TypeDesc>();
		}

		/// <inheritdoc />
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
		private readonly ModuleDesc _module;
		private readonly int _metadataToken;
		private readonly TypeDesc[] _instantiation;
		private readonly bool _isValueType;
		private readonly int _size;
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
		/// Declaring module
		/// </summary>
		public ModuleDesc Module => _module;

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken => _metadataToken;

		/// <summary>
		/// Type generic arguments (null if it is not a generic type)
		/// </summary>
		public TypeDesc[] Instantiation => _instantiation;

		/// <summary>
		/// Is value type
		/// </summary>
		public bool IsValueType => _isValueType;

		/// <summary>
		/// Type size (equals to sizeof(T))
		/// </summary>
		public int Size => _size;

		/// <summary>
		/// Loaded fields
		/// </summary>
		public IEnumerable<FieldDesc> Fields => _fields;

		/// <summary>
		/// Loaded methods
		/// </summary>
		public IEnumerable<MethodDesc> Methods => _methods;

		internal TypeDesc(ExecutionEngine executionEngine, Type reflType) {
			executionEngine.Context._types.Add(reflType, this);
			_executionEngine = executionEngine;
			_reflType = reflType;
			_module = executionEngine.ResolveModule(reflType.Module);
			_metadataToken = reflType.MetadataToken;
			_instantiation = reflType.IsGenericType ? reflType.GetGenericArguments().Select(executionEngine.ResolveType).ToArray() : Array.Empty<TypeDesc>();
			_isValueType = _reflType.IsValueType;
			_size = _isValueType ? SizeOf(reflType) : IntPtr.Size * 2;
			_fields = new List<FieldDesc>();
			_methods = new List<MethodDesc>();
		}

		/// <summary>
		/// Instantiates a generic type
		/// </summary>
		/// <param name="typeInstantiation"></param>
		/// <returns></returns>
		public TypeDesc Instantiate(params Type[] typeInstantiation) {
			if (typeInstantiation is null)
				throw new ExecutionEngineException(new ArgumentNullException(nameof(typeInstantiation)));

			Type reflType;
			try {
				reflType = _reflType.MakeGenericType(typeInstantiation);
			}
			catch (Exception ex) {
				throw new ExecutionEngineException(ex);
			}
			return _executionEngine.ResolveType(reflType);
		}

		/// <summary>
		/// Finds a method by token
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public MethodDesc FindMethod(int metadataToken) {
			const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			MethodBase reflMethod;
			try {
				reflMethod = _reflType.GetMethods(BINDING_FLAGS).FirstOrDefault(t => t.MetadataToken == metadataToken);
			}
			catch (Exception ex) {
				throw new ExecutionEngineException(ex);
			}
			return !(reflMethod is null) ? _executionEngine.ResolveMethod(reflMethod) : null;
		}

		private static int SizeOf(Type type) {
			var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), null, true);
			var generator = dynamicMethod.GetILGenerator();
			generator.Emit(OpCodes.Sizeof, type);
			generator.Emit(OpCodes.Ret);
			return (int)dynamicMethod.Invoke(null, null);
		}

		/// <inheritdoc />
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
		private readonly int _metadataToken;
		private readonly TypeDesc _declaringType;
		private readonly bool _isStatic;
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
		/// Declaring module
		/// </summary>
		public ModuleDesc Module => _declaringType.Module;

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken => _metadataToken;

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType => _declaringType;

		/// <summary>
		/// Is a static field
		/// </summary>
		public bool IsStatic => _isStatic;

		/// <summary>
		/// Field offset
		/// </summary>
		public uint Offset => _offset;

		/// <summary>
		/// Has <see cref="ThreadStaticAttribute"/>
		/// </summary>
		public bool IsThreadStatic => _isThreadStatic;

		internal FieldDesc(ExecutionEngine executionEngine, FieldInfo reflField) {
			executionEngine.Context._fields.Add(reflField, this);
			_executionEngine = executionEngine;
			_reflField = reflField;
			_metadataToken = reflField.MetadataToken;
			_declaringType = executionEngine.ResolveType(reflField.FieldType);
			_isStatic = reflField.IsStatic;
			_offset = !_isStatic ? GetFieldOffset((void*)reflField.FieldHandle.Value) : 0;
			_isThreadStatic = !(reflField.GetCustomAttribute<ThreadStaticAttribute>() is null);
		}

		private static uint GetFieldOffset(void* fieldHandle) {
			return *(uint*)((byte*)fieldHandle + sizeof(void*) + 4) & 0x7FFFFFF;
		}

		/// <inheritdoc />
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
		private readonly int _metadataToken;
		private readonly TypeDesc _declaringType;
		private readonly TypeDesc[] _instantiation;
		private readonly bool _isStatic;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Reflection method
		/// </summary>
		public MethodBase ReflMethod => _reflMethod;

		/// <summary>
		/// Declaring module
		/// </summary>
		public ModuleDesc Module => _declaringType.Module;

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken => _metadataToken;

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType => _declaringType;

		/// <summary>
		/// Method generic arguments (null if it is not a generic method)
		/// </summary>
		public TypeDesc[] Instantiation => _instantiation;

		/// <summary>
		/// Is a static method
		/// </summary>
		public bool IsStatic => _isStatic;

		internal MethodDesc(ExecutionEngine executionEngine, MethodBase reflMethod) {
			executionEngine.Context._methods.Add(reflMethod, this);
			_executionEngine = executionEngine;
			_reflMethod = reflMethod;
			_metadataToken = reflMethod.MetadataToken;
			_instantiation = reflMethod.IsGenericMethod ? reflMethod.GetGenericArguments().Select(executionEngine.ResolveType).ToArray() : Array.Empty<TypeDesc>();
			_declaringType = executionEngine.ResolveType(reflMethod.DeclaringType);
			_isStatic = reflMethod.IsStatic;
		}

		/// <summary>
		/// Instantiates a generic method
		/// </summary>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		public MethodDesc Instantiate(params Type[] methodInstantiation) {
			if (methodInstantiation is null)
				throw new ExecutionEngineException(new ArgumentNullException(nameof(methodInstantiation)));
			if (!(_reflMethod is MethodInfo reflMethodInfo))
				throw new ExecutionEngineException(new InvalidOperationException("Constructor can't be instantiated."));

			MethodInfo reflMethod;
			try {
				reflMethod = reflMethodInfo.MakeGenericMethod(methodInstantiation);
			}
			catch (Exception ex) {
				throw new ExecutionEngineException(ex);
			}
			return _executionEngine.ResolveMethod(reflMethod);
		}

		/// <summary>
		/// Instantiates a generic method
		/// </summary>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		public MethodDesc Instantiate(Type[] typeInstantiation, Type[] methodInstantiation) {
			var type = _declaringType;
			var method = this;
			if (!(typeInstantiation is null)) {
				type = type.Instantiate(typeInstantiation);
				method = type.FindMethod(_metadataToken);
			}
			if (!(methodInstantiation is null))
				method = method.Instantiate(methodInstantiation);
			return method;
		}

		/// <inheritdoc />
		public override string ToString() {
			return _reflMethod.ToString();
		}
	}
}
