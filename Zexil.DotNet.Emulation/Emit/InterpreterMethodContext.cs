using System;
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
				// TODO: localDefs to localTypes
				_localTypes = Array.Empty<TypeDesc>();
			}
			else {
				_localTypes = Array.Empty<TypeDesc>();
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
