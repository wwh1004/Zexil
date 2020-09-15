using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// See CorHdr.h/CorElementType (copied from dnlib)
	/// </summary>
	public enum ElementType : byte {
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
	/// Extension of <see cref="ElementType"/>
	/// </summary>
	[Flags]
	public enum AnnotatedElementType : uint {
		/// <summary>
		/// Small value type indicates the size of structure is less than or equal to size of native int (without this flag we regard it as large structure that can't be direct store)
		/// </summary>
		SmallValueType = 1 << 8
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
		public ExecutionEngine ExecutionEngine {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _executionEngine;
		}

		/// <summary>
		/// Reflection assembly
		/// </summary>
		[Obsolete("This property is just for quick test.")]
		public Assembly ReflAssembly {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _reflAssembly;
		}

		/// <summary>
		/// Assembly full name
		/// </summary>
		public string FullName {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _fullName;
		}

		/// <summary>
		/// Original assembly data
		/// </summary>
		public void* RawAssembly {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _rawAssembly;
		}

		/// <summary>
		/// Loaded modules
		/// </summary>
		public IEnumerable<ModuleDesc> Modules {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _modules;
		}

		/// <summary>
		/// Manifest module (the first module in <see cref="Modules"/>)
		/// </summary>
		public ModuleDesc ManifestModule {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _modules[0];
		}

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
		public ExecutionEngine ExecutionEngine {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _executionEngine;
		}

		/// <summary>
		/// Reflection module
		/// </summary>
		[Obsolete("This property is just for quick test.")]
		public Module ReflModule {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _reflModule;
		}

		/// <summary>
		/// Declaring assembly
		/// </summary>
		public AssemblyDesc Assembly {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _assembly;
		}

		/// <summary>
		/// Scope name
		/// </summary>
		public string ScopeName {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _scopeName;
		}

		/// <summary>
		/// Loaded types
		/// </summary>
		public IEnumerable<TypeDesc> Types {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _types;
		}

		/// <summary>
		/// Resolves a type and return runtime type
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TypeDesc ResolveType(int metadataToken) {
			var type = _reflModule.ResolveType(metadataToken);
			return _executionEngine.ResolveType(type);
		}

		/// <summary>
		/// Resolves a type and return runtime type
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Type ResolveReflType(int metadataToken) {
			return _reflModule.ResolveType(metadataToken);
		}

		/// <summary>
		/// Resolves a field and return runtime field
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FieldDesc ResolveField(int metadataToken) {
			var field = _reflModule.ResolveField(metadataToken);
			return _executionEngine.ResolveField(field);
		}

		/// <summary>
		/// Resolves a field and return runtime field
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public FieldInfo ResolveReflField(int metadataToken) {
			return _reflModule.ResolveField(metadataToken);
		}

		/// <summary>
		/// Resolves a method and return runtime method
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MethodDesc ResolveMethod(int metadataToken) {
			var method = _reflModule.ResolveMethod(metadataToken);
			return _executionEngine.ResolveMethod(method);
		}

		/// <summary>
		/// Resolves a method and return runtime method
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MethodBase ResolveReflMethod(int metadataToken) {
			return _reflModule.ResolveMethod(metadataToken);
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
		internal readonly Type _reflType;
		private readonly ModuleDesc _module;
		private readonly int _metadataToken;
		private readonly TypeDesc[] _instantiation;
		private readonly int _size;
		private readonly AnnotatedElementType _elementType;
		private readonly bool _isCOMObject;
		private readonly int _genericParameterIndex;
		internal readonly List<FieldDesc> _fields;
		internal readonly List<MethodDesc> _methods;
		private bool _allMethodsResolvedAndSorted;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _executionEngine;
		}

		/// <summary>
		/// Reflection type
		/// </summary>
		[Obsolete("This property is just for quick test.")]
		public Type ReflType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _reflType;
		}

		/// <summary>
		/// Declaring module
		/// </summary>
		public ModuleDesc Module {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _module;
		}

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _metadataToken;
		}

		/// <summary>
		/// Type generic arguments (null if it is not a generic type)
		/// </summary>
		public TypeDesc[] Instantiation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instantiation;
		}

		/// <summary>
		/// Type size (equals to sizeof(T))
		/// </summary>
		public int Size {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _size;
		}

		/// <summary>
		/// ElementType
		/// </summary>
		public ElementType ElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ElementType)_elementType;
		}

		/// <summary>
		/// AnnotatedElementType
		/// </summary>
		public AnnotatedElementType AnnotatedElementType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _elementType;
		}

		/// <summary>
		/// Is ByRef type
		/// </summary>
		public bool IsByRef {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.ByRef;
		}

		/// <summary>
		/// Is pointer type
		/// </summary>
		public bool IsPointer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.Ptr;
		}

		/// <summary>
		/// Is primitive type
		/// </summary>
		public bool IsPrimitive {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ElementType >= ElementType.Boolean && ElementType <= ElementType.R8) || (ElementType >= ElementType.I && ElementType <= ElementType.R);
		}

		/// <summary>
		/// Is value type
		/// </summary>
		public bool IsValueType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (ElementType >= ElementType.Boolean && ElementType <= ElementType.R8) || (ElementType >= ElementType.ValueArray && ElementType <= ElementType.R) || ElementType == ElementType.ValueType;
		}

		/// <summary>
		/// Is generic parameter
		/// </summary>
		public bool IsGenericParameter {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.Var || ElementType == ElementType.MVar;
		}

		/// <summary>
		/// Is generic type parameter
		/// </summary>
		public bool IsGenericTypeParameter {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.Var;
		}

		/// <summary>
		/// Is generic method parameter
		/// </summary>
		public bool IsGenericMethodParameter {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => ElementType == ElementType.MVar;
		}

		/// <summary>
		/// Is a COM object
		/// </summary>
		public bool IsCOMObject {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isCOMObject;
		}

		/// <summary>
		/// Generic parameter index
		/// </summary>
		public int GenericParameterIndex {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _genericParameterIndex;
		}

		/// <summary>
		/// Loaded fields
		/// </summary>
		public IEnumerable<FieldDesc> Fields {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _fields;
		}

		/// <summary>
		/// Loaded methods
		/// </summary>
		public IEnumerable<MethodDesc> Methods {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _methods;
		}

		internal TypeDesc(ExecutionEngine executionEngine, Type reflType) {
			executionEngine.Context._types.Add(reflType, this);
			_executionEngine = executionEngine;
			_reflType = reflType;
			_module = executionEngine.ResolveModule(reflType.Module);
			_metadataToken = reflType.MetadataToken;
			_instantiation = reflType.IsGenericType ? reflType.GetGenericArguments().Select(t => executionEngine.ResolveType(t)).ToArray() : Array.Empty<TypeDesc>();
			_size = IsValueType ? SizeOf(reflType) : IntPtr.Size;
			_elementType = (AnnotatedElementType)GetElementType();
			// not redundant code!!! GetAnnotatedElementType will use _elementType field
			_elementType = GetAnnotatedElementType();
			_isCOMObject = _reflType.IsCOMObject;
			_genericParameterIndex = IsGenericParameter ? reflType.GenericParameterPosition : 0;
			_fields = new List<FieldDesc>();
			_methods = new List<MethodDesc>();
		}

		/// <summary>
		/// Instantiates a generic type
		/// </summary>
		/// <param name="typeInstantiation"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
			foreach (var method in _methods) {
				if (method.MetadataToken == metadataToken)
					return method;
			}
			var typeInfo = (TypeInfo)_reflType;
			foreach (var methodInfo in typeInfo.DeclaredMethods) {
				if (methodInfo.MetadataToken == metadataToken)
					return CreateMethodFast(methodInfo);
			}
			// we assume method is more widespread than constructor
			foreach (var constructorInfo in typeInfo.DeclaredConstructors) {
				if (constructorInfo.MetadataToken == metadataToken)
					return CreateMethodFast(constructorInfo);
			}
			return null;
		}

		/// <summary>
		/// Finds a method by token (faster than <see cref="FindMethod(int)"/> for we know if it is a constructor)
		/// </summary>
		/// <param name="methodDefinition">Generic method definition</param>
		/// <returns></returns>
		public MethodDesc FindMethod(MethodDesc methodDefinition) {
			if (methodDefinition is null)
				throw new ArgumentNullException(nameof(methodDefinition));

#if DEBUG
			System.Diagnostics.Debug.Assert(_instantiation.Length > 0);
#endif

			foreach (var method in _methods) {
				if (method.MetadataToken == methodDefinition.MetadataToken)
					return method;
			}
			var typeInfo = (TypeInfo)_reflType;
			if (methodDefinition.IsConstructor) {
				foreach (var constructorInfo in typeInfo.DeclaredConstructors) {
					if (constructorInfo.MetadataToken == methodDefinition.MetadataToken)
						return CreateMethodFast(constructorInfo);
				}
			}
			else {
				foreach (var methodInfo in typeInfo.DeclaredMethods) {
					if (methodInfo.MetadataToken == methodDefinition.MetadataToken)
						return CreateMethodFast(methodInfo);
				}
			}
			return null;
		}

		/// <summary>
		/// Finds a method by token that will resolve all methods first then do a binary search target method
		/// </summary>
		/// <param name="metadataToken"></param>
		/// <returns></returns>
		public MethodDesc FindMethodCaching(int metadataToken) {
			if (!_allMethodsResolvedAndSorted) {
				ResolveAndSortAllMethods();
				_allMethodsResolvedAndSorted = true;
			}
			int lo = 0;
			int hi = 0 + _methods.Count - 1;
			while (lo <= hi) {
				int i = lo + ((hi - lo) >> 1);
				int order = _methods[i].MetadataToken - metadataToken;
				if (order == 0)
					return _methods[i];
				if (order < 0)
					lo = i + 1;
				else
					hi = i - 1;
			}
			return null;
		}

		/// <summary>
		/// Gets actual generic parameter if <see cref="IsGenericParameter"/>
		/// </summary>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public TypeDesc ResolveInstantiation(TypeDesc[] typeInstantiation, TypeDesc[] methodInstantiation) {
			if (IsGenericTypeParameter && typeInstantiation is null)
				throw new ArgumentNullException(nameof(typeInstantiation));
			if (IsGenericMethodParameter && methodInstantiation is null)
				throw new ArgumentNullException(nameof(methodInstantiation));

			switch (ElementType) {
			case ElementType.Var: return typeInstantiation[_genericParameterIndex];
			case ElementType.MVar: return methodInstantiation[_genericParameterIndex];
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

		private ElementType GetElementType() {
			object boxedValue = !CLREnvironment.IsFramework2x ? _getCorElementType.Invoke(null, new object[] { _reflType }) : _getCorElementType.Invoke(_reflType.TypeHandle, null);
			var elementType = (ElementType)(byte)boxedValue;
			return elementType;
		}

		private AnnotatedElementType GetAnnotatedElementType() {
			var annotatedElementType = _elementType;
			if (IsValueType && _size <= IntPtr.Size)
				annotatedElementType |= AnnotatedElementType.SmallValueType;
			return annotatedElementType;
		}

		private void ResolveAndSortAllMethods() {
			var typeInfo = (TypeInfo)_reflType;
			foreach (var constructorInfo in typeInfo.DeclaredConstructors)
				ResolveMethodFast(constructorInfo);
			foreach (var methodInfo in typeInfo.DeclaredMethods)
				ResolveMethodFast(methodInfo);
			_methods.Sort((x, y) => x.MetadataToken - y.MetadataToken);
		}

		private MethodDesc ResolveMethodFast(MethodBase method) {
			if (_executionEngine.Context._methods.TryGetValue(method, out var methodDesc))
				return methodDesc;
			return CreateMethodFast(method);
		}

		private MethodDesc CreateMethodFast(MethodBase method) {
			var methodDesc = new MethodDesc(_executionEngine, method);
			_methods.Add(methodDesc);
			return methodDesc;
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
		public ExecutionEngine ExecutionEngine {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _executionEngine;
		}

		/// <summary>
		/// Reflection field
		/// </summary>
		[Obsolete("This property is just for quick test.")]
		public FieldInfo ReflField {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _reflField;
		}

		/// <summary>
		/// Declaring module
		/// </summary>
		public ModuleDesc Module {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _declaringType.Module;
		}

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _metadataToken;
		}

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _declaringType;
		}

		/// <summary>
		/// Is a static field
		/// </summary>
		public bool IsStatic {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isStatic;
		}

		/// <summary>
		/// Field offset
		/// </summary>
		public uint Offset {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _offset;
		}

		/// <summary>
		/// Has <see cref="ThreadStaticAttribute"/>
		/// </summary>
		public bool IsThreadStatic {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isThreadStatic;
		}

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
		[Flags]
		private enum MethodFlags {
			None = 0,
			StaticConstructor,
			InstanceConstructor,
			ConstructorMask = 3,
		}

		private readonly ExecutionEngine _executionEngine;
		private readonly MethodBase _reflMethod;
		private readonly int _metadataToken;
		private readonly TypeDesc _declaringType;
		private readonly TypeDesc[] _instantiation;
		private readonly MethodAttributes _attributes;
		private readonly MethodFlags _flags;
		private TypeDesc[] _parameters;
		private readonly TypeDesc _returnType;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _executionEngine;
		}

		/// <summary>
		/// Reflection method
		/// </summary>
		[Obsolete("This property is just for quick test.")]
		public MethodBase ReflMethod {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _reflMethod;
		}

		/// <summary>
		/// Declaring module
		/// </summary>
		public ModuleDesc Module {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _declaringType.Module;
		}

		/// <summary>
		/// Metadata token
		/// </summary>
		public int MetadataToken {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _metadataToken;
		}

		/// <summary>
		/// Declaring type
		/// </summary>
		public TypeDesc DeclaringType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _declaringType;
		}

		/// <summary>
		/// Method generic arguments (null if it is not a generic method)
		/// </summary>
		public TypeDesc[] Instantiation {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _instantiation;
		}

		/// <summary>
		/// Method attributes
		/// </summary>
		public MethodAttributes Attributes {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _attributes;
		}

		/// <summary>
		/// Has <see cref="MethodAttributes.Static"/>
		/// </summary>
		public bool IsStatic {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_attributes & MethodAttributes.Static) != 0;
		}

		/// <summary>
		/// Has <see cref="MethodAttributes.SpecialName"/>
		/// </summary>
		public bool IsSpecialName {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_attributes & MethodAttributes.SpecialName) != 0;
		}

		/// <summary>
		/// Has <see cref="MethodAttributes.RTSpecialName"/>
		/// </summary>
		public bool IsRuntimeSpecialName {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_attributes & MethodAttributes.RTSpecialName) != 0;
		}

		/// <summary>
		/// Is static constructor (.cctor)
		/// </summary>
		public bool IsStaticConstructor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_flags & MethodFlags.StaticConstructor) != 0;
		}

		/// <summary>
		/// Is instance constructor (.ctor)
		/// </summary>
		public bool IsInstanceConstructor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_flags & MethodFlags.InstanceConstructor) != 0;
		}

		/// <summary>
		/// Is constructor (.cctor or .ctor)
		/// </summary>
		public bool IsConstructor {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => (_flags & (MethodFlags.StaticConstructor | MethodFlags.InstanceConstructor)) != 0;
		}

		/// <summary>
		/// Method parameters including "this" pointer
		/// </summary>
		public TypeDesc[] Parameters {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (_parameters is null)
					_parameters = GetParameters();
				return _parameters;
			}
		}

		/// <summary>
		/// Method return type
		/// </summary>
		public TypeDesc ReturnType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _returnType;
		}

		/// <summary>
		/// Does method have return type
		/// </summary>
		public bool HasReturnType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _returnType.ElementType != ElementType.Void;
		}

		internal MethodDesc(ExecutionEngine executionEngine, MethodBase reflMethod) {
			executionEngine.Context._methods.Add(reflMethod, this);
			_executionEngine = executionEngine;
			_reflMethod = reflMethod;
			_metadataToken = reflMethod.MetadataToken;
			_instantiation = reflMethod.IsGenericMethod ? reflMethod.GetGenericArguments().Select(t => executionEngine.ResolveType(t)).ToArray() : Array.Empty<TypeDesc>();
			_declaringType = executionEngine.ResolveType(reflMethod.DeclaringType);
			_attributes = reflMethod.Attributes;
			_flags = GetConstructorFlags();
			_returnType = executionEngine.ResolveType((reflMethod is MethodInfo methodInfo) ? methodInfo.ReturnType : typeof(void));
		}

		private MethodFlags GetConstructorFlags() {
			if (!IsRuntimeSpecialName)
				return MethodFlags.None;
			string name = _reflMethod.Name;
			if (name == ".cctor")
				return MethodFlags.StaticConstructor;
			else if (name == ".ctor")
				return MethodFlags.InstanceConstructor;
			else
				return MethodFlags.None;
		}

		private TypeDesc[] GetParameters() {
			var reflParameters = _reflMethod.GetParameters();
			var parameters = new List<TypeDesc>(reflParameters.Length + 1);
			if (!IsStatic)
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public MethodDesc Instantiate(Type[] typeInstantiation, Type[] methodInstantiation) {
			var type = _declaringType;
			var method = this;
			if (!(typeInstantiation is null)) {
				type = type.Instantiate(typeInstantiation);
				method = type.FindMethod(this);
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
