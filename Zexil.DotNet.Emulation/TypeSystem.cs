using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// See CorHdr.h/CorElementType (copied from dnlib)
	/// </summary>
	public enum CorElementType : byte {
		/// <summary />
		End = 0x00,
		/// <summary>System.Void</summary>
		Void = 0x01,
		/// <summary>System.Boolean</summary>
		Boolean = 0x02,
		/// <summary>System.Char</summary>
		Char = 0x03,
		/// <summary>System.SByte</summary>
		I1 = 0x04,
		/// <summary>System.Byte</summary>
		U1 = 0x05,
		/// <summary>System.Int16</summary>
		I2 = 0x06,
		/// <summary>System.UInt16</summary>
		U2 = 0x07,
		/// <summary>System.Int32</summary>
		I4 = 0x08,
		/// <summary>System.UInt32</summary>
		U4 = 0x09,
		/// <summary>System.Int64</summary>
		I8 = 0x0A,
		/// <summary>System.UInt64</summary>
		U8 = 0x0B,
		/// <summary>System.Single</summary>
		R4 = 0x0C,
		/// <summary>System.Double</summary>
		R8 = 0x0D,
		/// <summary>System.String</summary>
		String = 0x0E,
		/// <summary>Pointer type (*)</summary>
		Ptr = 0x0F,
		/// <summary>ByRef type (&amp;)</summary>
		ByRef = 0x10,
		/// <summary>Value type</summary>
		ValueType = 0x11,
		/// <summary>Reference type</summary>
		Class = 0x12,
		/// <summary>Type generic parameter</summary>
		Var = 0x13,
		/// <summary>Multidimensional array ([*], [,], [,,], ...)</summary>
		Array = 0x14,
		/// <summary>Generic instance type</summary>
		GenericInst = 0x15,
		/// <summary>Typed byref</summary>
		TypedByRef = 0x16,
		/// <summary>Value array (don't use)</summary>
		ValueArray = 0x17,
		/// <summary>System.IntPtr</summary>
		I = 0x18,
		/// <summary>System.UIntPtr</summary>
		U = 0x19,
		/// <summary>native real (don't use)</summary>
		R = 0x1A,
		/// <summary>Function pointer</summary>
		FnPtr = 0x1B,
		/// <summary>System.Object</summary>
		Object = 0x1C,
		/// <summary>Single-dimension, zero lower bound array ([])</summary>
		SZArray = 0x1D,
		/// <summary>Method generic parameter</summary>
		MVar = 0x1E,
		/// <summary>Required C modifier</summary>
		CModReqd = 0x1F,
		/// <summary>Optional C modifier</summary>
		CModOpt = 0x20,
		/// <summary>Used internally by the CLR (don't use)</summary>
		Internal = 0x21,
		/// <summary>Module (don't use)</summary>
		Module = 0x3F,
		/// <summary>Sentinel (method sigs only)</summary>
		Sentinel = 0x41,
		/// <summary>Pinned type (locals only)</summary>
		Pinned = 0x45
	}

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
		private static readonly MethodInfo _getCorElementType = typeof(RuntimeTypeHandle).GetMethod("GetCorElementType", BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

		private readonly ExecutionEngine _executionEngine;
		private readonly Type _reflType;
		private readonly ModuleDesc _module;
		private readonly int _metadataToken;
		private readonly TypeDesc[] _instantiation;
		private readonly CorElementType _elementType;
		private readonly bool _isCOMObject;
		private readonly int _size;
		private readonly int _genericParameterIndex;
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
		/// CorElementType
		/// </summary>
		public CorElementType ElementType => _elementType;

		/// <summary>
		/// Is ByRef type
		/// </summary>
		public bool IsByRef => _elementType == CorElementType.ByRef;

		/// <summary>
		/// Is pointer type
		/// </summary>
		public bool IsPointer => _elementType == CorElementType.Ptr;

		/// <summary>
		/// Is primitive type
		/// </summary>
		public bool IsPrimitive => (_elementType >= CorElementType.Boolean && _elementType <= CorElementType.R8) || (_elementType >= CorElementType.I && _elementType <= CorElementType.R);

		/// <summary>
		/// Is value type
		/// </summary>
		public bool IsValueType => (_elementType >= CorElementType.Boolean && _elementType <= CorElementType.R8) || (_elementType >= CorElementType.ValueArray && _elementType <= CorElementType.R) || _elementType == CorElementType.ValueType;

		/// <summary>
		/// Is generic parameter
		/// </summary>
		public bool IsGenericParameter => _elementType == CorElementType.Var || _elementType == CorElementType.MVar;

		/// <summary>
		/// Is generic type parameter
		/// </summary>
		public bool IsGenericTypeParameter => _elementType == CorElementType.Var;

		/// <summary>
		/// Is generic method parameter
		/// </summary>
		public bool IsGenericMethodParameter => _elementType == CorElementType.MVar;

		/// <summary>
		/// Is a COM object
		/// </summary>
		public bool IsCOMObject => _isCOMObject;

		/// <summary>
		/// Type size (equals to sizeof(T))
		/// </summary>
		public int Size => _size;

		/// <summary>
		/// Generic parameter index
		/// </summary>
		public int GenericParameterIndex => _genericParameterIndex;

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
			_instantiation = reflType.IsGenericType ? reflType.GetGenericArguments().Select(t => executionEngine.ResolveType(t)).ToArray() : Array.Empty<TypeDesc>();
			_elementType = GetCorElementType();
			_isCOMObject = _reflType.IsCOMObject;
			_size = IsValueType ? SizeOf(reflType) : IntPtr.Size;
			_genericParameterIndex = IsGenericParameter ? reflType.GenericParameterPosition : 0;
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
				throw new ArgumentNullException(nameof(typeInstantiation));

			var reflType = _reflType.MakeGenericType(typeInstantiation);
			return _executionEngine.ResolveType(reflType);
		}

		/// <summary>
		/// Finds a method by token
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public MethodDesc FindMethod(int metadataToken) {
			const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

			var reflMethod = _reflType.GetMethods(BINDING_FLAGS).FirstOrDefault(t => t.MetadataToken == metadataToken);
			return !(reflMethod is null) ? _executionEngine.ResolveMethod(reflMethod) : null;
		}

		/// <summary>
		/// Gets actual generic parameter if <see cref="IsGenericParameter"/>
		/// </summary>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		public TypeDesc ResolveInstantiation(TypeDesc[] typeInstantiation, TypeDesc[] methodInstantiation) {
			if (IsGenericTypeParameter && typeInstantiation is null)
				throw new ArgumentNullException(nameof(typeInstantiation));
			if (IsGenericMethodParameter && methodInstantiation is null)
				throw new ArgumentNullException(nameof(methodInstantiation));

			switch (_elementType) {
			case CorElementType.Var: return typeInstantiation[_genericParameterIndex];
			case CorElementType.MVar: return methodInstantiation[_genericParameterIndex];
			default: return this;
			}
		}

		private static int SizeOf(Type type) {
			var dynamicMethod = new DynamicMethod(string.Empty, typeof(int), null, true);
			var generator = dynamicMethod.GetILGenerator();
			generator.Emit(OpCodes.Sizeof, type);
			generator.Emit(OpCodes.Ret);
			return (int)dynamicMethod.Invoke(null, null);
		}

		private CorElementType GetCorElementType() {
			object boxedValue = !CLREnvironment.IsFramework2x ? _getCorElementType.Invoke(null, new object[] { _reflType }) : _getCorElementType.Invoke(_reflType.TypeHandle, null);
			return (CorElementType)(byte)boxedValue;
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
		private readonly TypeDesc[] _parameters;
		private readonly TypeDesc _returnType;

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

		/// <summary>
		/// Method parameters including "this" pointer
		/// </summary>
		public TypeDesc[] Parameters => _parameters;

		/// <summary>
		/// Method return type
		/// </summary>
		public TypeDesc ReturnType => _returnType;

		/// <summary>
		/// Does method have return type
		/// </summary>
		public bool HasReturnType => _returnType.ElementType != CorElementType.Void;

		internal MethodDesc(ExecutionEngine executionEngine, MethodBase reflMethod) {
			executionEngine.Context._methods.Add(reflMethod, this);
			_executionEngine = executionEngine;
			_reflMethod = reflMethod;
			_metadataToken = reflMethod.MetadataToken;
			_instantiation = reflMethod.IsGenericMethod ? reflMethod.GetGenericArguments().Select(t => executionEngine.ResolveType(t)).ToArray() : Array.Empty<TypeDesc>();
			_declaringType = executionEngine.ResolveType(reflMethod.DeclaringType);
			_isStatic = reflMethod.IsStatic;
			_parameters = GetParameters();
			_returnType = executionEngine.ResolveType((reflMethod is MethodInfo methodInfo) ? methodInfo.ReturnType : typeof(void));
		}

		private TypeDesc[] GetParameters() {
			var reflParameters = _reflMethod.GetParameters();
			var parameters = new List<TypeDesc>(reflParameters.Length + 1);
			if (!_isStatic)
				parameters.Add(_declaringType);
			foreach (var reflParameter in reflParameters)
				parameters.Add(_executionEngine.ResolveType(reflParameter.ParameterType));
			return parameters.ToArray();
		}

		/// <summary>
		/// Instantiates a generic method
		/// </summary>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		public MethodDesc Instantiate(params Type[] methodInstantiation) {
			if (methodInstantiation is null)
				throw new ArgumentNullException(nameof(methodInstantiation));
			if (!(_reflMethod is MethodInfo reflMethodInfo))
				throw new InvalidOperationException("Constructor can't be instantiated.");

			var reflMethod = reflMethodInfo.MakeGenericMethod(methodInstantiation);
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
