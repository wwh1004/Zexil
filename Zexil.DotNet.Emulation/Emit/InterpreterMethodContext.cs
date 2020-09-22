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
		private InterpreterSlot* _stackBase;
		private InterpreterSlot* _stack;
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
		public InterpreterSlot* StackBase {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _stackBase;
		}

		/// <summary>
		/// Stack top
		/// </summary>
		public InterpreterSlot* Stack {
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
					System.Diagnostics.Debug.Assert(localDefs[i].Index == i);
#endif

				_localTypes = new TypeDesc[localDefs.Count];
				for (int i = 0; i < _localTypes.Length; i++)
					_localTypes[i] = _context.ExecutionEngine.ResolveType(DnlibHelpers.TypeSigToReflTypeLocal(_method, localDefs[i].Type));
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
			_stackBase = _context.AcquireStack();
			_stack = _stackBase + InterpreterContext.StackSize;
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
			_stackBase = null;
			_stack = null;
			_isDisposed = true;
		}

		#region public stack apis
		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushI4(int value) {
			ref var slot = ref *--Stack;
			slot.I4 = value;
			slot.ElementType = ElementType.I4;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushI8(long value) {
			ref var slot = ref *--Stack;
			slot.I8 = value;
			slot.ElementType = ElementType.I8;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushI(void* value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.ElementType = ElementType.I;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushByRef(void* value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.ElementType = ElementType.ByRef;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushR4(float value) {
			ref var slot = ref *--Stack;
			slot.R4 = value;
			slot.ElementType = ElementType.R4;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushR8(double value) {
			ref var slot = ref *--Stack;
			slot.R8 = value;
			slot.ElementType = ElementType.R8;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(void* value, AnnotatedElementType annotatedElementType) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = annotatedElementType;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(void* value, ElementType elementType) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.ElementType = elementType;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(in InterpreterSlot value) {
			*--Stack = value;
		}

		/// <summary>
		/// Pops value from stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref InterpreterSlot Pop() {
			return ref *Stack++;
		}

		/// <summary>
		/// Peeks value on stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref InterpreterSlot Peek() {
			return ref *_stack;
		}

		/// <summary>
		/// Peeks value on stack
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref InterpreterSlot Peek(int index) {
#if DEBUG
			var stack = _stack + index;
			if (stack < _stackBase || stack >= _stackBase + InterpreterContext.StackSize)
				throw new OutOfMemoryException();
			return ref *stack;
#else
			return ref _stack[index];
#endif
		}
		#endregion

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed)
				ReleaseDynamicContext();
		}
	}
}
