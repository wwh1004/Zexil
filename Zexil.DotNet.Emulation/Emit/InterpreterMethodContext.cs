using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// CIL instruction interpreter method context
	/// </summary>
	public sealed unsafe class InterpreterMethodContext : IDisposable {
		#region Static Context
		private readonly InterpreterContext _context;
		private readonly MethodDesc _method;
		private readonly MethodDef _methodDef;
		private readonly TypeDesc[] _argumentTypes;
		private readonly TypeDesc[] _localTypes;

		/// <summary>
		/// Interpreted method
		/// </summary>
		public MethodDesc Method => _method;

		/// <summary>
		/// Type generic arguments
		/// </summary>
		public TypeDesc[] TypeInstantiation => _method.DeclaringType.Instantiation;

		/// <summary>
		/// Method generic arguments
		/// </summary>
		public TypeDesc[] MethodInstantiation => _method.Instantiation;

		/// <summary>
		/// Related <see cref="dnlib.DotNet.MethodDef"/>
		/// </summary>
		public MethodDef MethodDef => _methodDef;

		/// <summary>
		/// Argument types
		/// </summary>
		public TypeDesc[] ArgumentTypes => _argumentTypes;

		/// <summary>
		/// Local variable types
		/// </summary>
		public TypeDesc[] LocalTypes => _localTypes;
		#endregion

		#region Dynamic Context
		private void*[] _arguments;
		private void*[] _locals;
		private byte** _stackBase;
		private byte** _stack;
		private ElementType* _typeStackBase;
		private ElementType* _typeStack;
		private bool _isDisposed;

		/// <summary>
		/// Arguments (includes return buffer)
		/// </summary>
		public void*[] Arguments {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _arguments;
		}

		/// <summary>
		/// Local variables
		/// </summary>
		public void*[] Locals {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _locals;
		}

		/// <summary>
		/// Return buffer (pointer to return value)
		/// </summary>
		public void* ReturnBuffer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _method.HasReturnType ? _arguments[_arguments.Length - 1] : null;
		}

		/// <summary>
		/// Stack base
		/// </summary>
		public byte** StackBase {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _stackBase;
		}

		/// <summary>
		/// Stack top
		/// </summary>
		public byte** Stack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _stack;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
#if DEBUG
				if (value < _stackBase || value > _stackBase + InterpreterContext.StackSize)
					throw new OutOfMemoryException();
#endif
				_stack = value;
			}
		}

		/// <summary>
		/// Type stack base
		/// </summary>
		public ElementType* TypeStackBase {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _typeStackBase;
		}

		/// <summary>
		/// Type stack
		/// </summary>
		public ElementType* TypeStack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _typeStack;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
#if DEBUG
				if (value < _typeStackBase || value > _typeStackBase + InterpreterContext.TypeStackSize)
					throw new OutOfMemoryException();
#endif
				_typeStack = value;
			}
		}
		#endregion

		/// <summary>
		/// Without <see cref="Method"/> argument, <see cref="InterpreterMethodContext"/> shouldn't be cached.
		/// </summary>
		/// <param name="context"></param>
		internal InterpreterMethodContext(InterpreterContext context) {
			_context = context;
		}

		internal InterpreterMethodContext(InterpreterContext context, MethodDesc method, ModuleDef moduleDef) {
			_context = context;
			_method = method;
			_methodDef = (MethodDef)moduleDef.ResolveToken(method.MetadataToken);
			_argumentTypes = method.Parameters;
			if (_methodDef.HasBody) {
				var localDefs = _methodDef.Body.Variables;
#if DEBUG
				for (int i = 0; i < localDefs.Count; i++)
					Debug.Assert(localDefs[i].Index == i);
#endif

				_localTypes = new TypeDesc[localDefs.Count];
				for (int i = 0; i < _localTypes.Length; i++)
					_localTypes[i] = Import(localDefs[i].Type);
			}
			else {
				_localTypes = Array.Empty<TypeDesc>();
			}
		}

		private TypeDesc Import(TypeSig typeSig) {
			return _context.ExecutionEngine.ResolveType(ImportImpl(typeSig.RemovePinnedAndModifiers()));
		}

		private Type ImportImpl(TypeSig typeSig) {
			switch (typeSig.ElementType) {
			case dnlib.DotNet.ElementType.Void: return typeof(void);
			case dnlib.DotNet.ElementType.Boolean: return typeof(bool);
			case dnlib.DotNet.ElementType.Char: return typeof(char);
			case dnlib.DotNet.ElementType.I1: return typeof(sbyte);
			case dnlib.DotNet.ElementType.U1: return typeof(byte);
			case dnlib.DotNet.ElementType.I2: return typeof(short);
			case dnlib.DotNet.ElementType.U2: return typeof(ushort);
			case dnlib.DotNet.ElementType.I4: return typeof(int);
			case dnlib.DotNet.ElementType.U4: return typeof(uint);
			case dnlib.DotNet.ElementType.I8: return typeof(long);
			case dnlib.DotNet.ElementType.U8: return typeof(ulong);
			case dnlib.DotNet.ElementType.R4: return typeof(float);
			case dnlib.DotNet.ElementType.R8: return typeof(double);
			case dnlib.DotNet.ElementType.String: return typeof(string);
			case dnlib.DotNet.ElementType.Ptr: return ImportImpl(typeSig.Next).MakePointerType();
			case dnlib.DotNet.ElementType.ByRef: return ImportImpl(typeSig.Next).MakeByRefType();
			case dnlib.DotNet.ElementType.ValueType:
			case dnlib.DotNet.ElementType.Class: return _method.DeclaringType.Module.ResolveReflType(((TypeDefOrRefSig)typeSig).TypeDefOrRef.MDToken.ToInt32());
			case dnlib.DotNet.ElementType.Var: return _method.DeclaringType.Instantiation[((GenericVar)typeSig).Number]._reflType;
			case dnlib.DotNet.ElementType.Array: return ImportImpl(typeSig.Next).MakeArrayType((int)((ArraySig)typeSig).Rank);
			case dnlib.DotNet.ElementType.TypedByRef: return ImportImpl(typeSig.Next).MakeByRefType();
			case dnlib.DotNet.ElementType.I: return typeof(IntPtr);
			case dnlib.DotNet.ElementType.U: return typeof(UIntPtr);
			case dnlib.DotNet.ElementType.R: return typeof(double);
			case dnlib.DotNet.ElementType.FnPtr: return typeof(IntPtr);
			case dnlib.DotNet.ElementType.Object: return typeof(object);
			case dnlib.DotNet.ElementType.SZArray: return ImportImpl(typeSig.Next).MakeArrayType();
			case dnlib.DotNet.ElementType.MVar: return _method.Instantiation[((GenericMVar)typeSig).Number]._reflType;
			case dnlib.DotNet.ElementType.End:
			case dnlib.DotNet.ElementType.GenericInst:
			case dnlib.DotNet.ElementType.ValueArray:
			case dnlib.DotNet.ElementType.CModReqd:
			case dnlib.DotNet.ElementType.CModOpt:
			case dnlib.DotNet.ElementType.Internal:
			case dnlib.DotNet.ElementType.Module:
			case dnlib.DotNet.ElementType.Sentinel:
			case dnlib.DotNet.ElementType.Pinned: throw new NotSupportedException();
			default: throw new InvalidOperationException("Unreachable");
			}
		}

		internal void ResolveDynamicContext(void*[] arguments) {
			if (!(_method is null)) {
				if (arguments is null)
					throw new ArgumentNullException(nameof(arguments));
				if (arguments.Length != _method.Parameters.Length + (_method.HasReturnType ? 1 : 0))
					throw new ArgumentException(nameof(arguments));

				_arguments = arguments;
				_locals = new void*[_localTypes.Length];
			}
			_stackBase = (byte**)_context.AcquireStack();
			_stack = _stackBase + InterpreterContext.StackSize;
			_typeStackBase = (ElementType*)_context.AcquireTypeStack();
			_typeStack = _typeStackBase + InterpreterContext.TypeStackSize;
			_isDisposed = false;
		}

		internal void ReleaseDynamicContext() {
			if (_isDisposed)
				throw new InvalidOperationException();

			if (!(_method is null)) {
				_arguments = null;
				_locals = null;
				_context.ReleaseMethodContext(this);
			}
			_context.ReleaseStack(_stackBase);
			_context.ReleaseTypeStack(_typeStackBase);
			_stackBase = null;
			_stack = null;
			_typeStackBase = null;
			_typeStack = null;
			_isDisposed = true;
		}

		/// <summary>
		/// Pushes object onto stack top
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushObject<T>(T obj) where T : class {
			Push(JitHelpers.AsPointer(obj));
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(void* value) {
			*--Stack = (byte*)value;
		}

		/// <summary>
		/// Pops object from stack top
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T PopObject<T>() where T : class {
			return JitHelpers.As<T>(Pop());
		}

		/// <summary>
		/// Pops object reference from stack top
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T PopObjectRef<T>() {
			return ref Unsafe.AsRef<T>(Pop());
		}

		/// <summary>
		/// Pops value from stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void* Pop() {
			return *Stack++;
		}

		/// <summary>
		/// Peeks object on stack top
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public T PeekObject<T>() where T : class {
			return JitHelpers.As<T>(Peek());
		}

		/// <summary>
		/// Peeks object reference on stack top
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref T PeekObjectRef<T>() {
			return ref Unsafe.AsRef<T>(Peek());
		}

		/// <summary>
		/// Peeks value on stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void* Peek() {
			return *Stack;
		}

		/// <summary>
		/// Pushes type onto type stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(ElementType type) {
			*--TypeStack = type;
		}

		/// <summary>
		/// Pops type on type stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ElementType PopType() {
			return *TypeStack++;
		}

		/// <summary>
		/// Peeks type on type stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ElementType PeekType() {
			return *TypeStack;
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed)
				ReleaseDynamicContext();
		}
	}
}
