using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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
		/// Interpreted module
		/// </summary>
		public ModuleDesc Module => _method.Module;

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
		private InterpreterSlot[] _arguments;
		private InterpreterSlot[] _locals;
		private InterpreterSlot* _stackBase;
		private InterpreterSlot* _stack;
		private Stack<GCHandle> _handles;
		private GCHandle _lastUsedHandle;
		private List<nint> _stackAlloceds;
		private bool _isConstrainedValueType;
		private uint? _nextILOffset;
		private bool _isReturned;
		private bool _isDisposed;

		/// <summary>
		/// Arguments (includes return buffer)
		/// </summary>
		public InterpreterSlot[] Arguments {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _arguments;
		}

		/// <summary>
		/// Local variables
		/// </summary>
		public InterpreterSlot[] Locals {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _locals;
		}

		/// <summary>
		/// Return buffer (pointer to return value)
		/// </summary>
		public ref InterpreterSlot ReturnBuffer {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get {
				if (!_method.HasReturnType)
					throw new InvalidOperationException();

				return ref _arguments[_arguments.Length - 1];
			}
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

		/// <summary>
		/// Handles of objects
		/// </summary>
		internal Stack<GCHandle> Handles {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _handles;
		}

		/// <summary>
		/// Last used object handle
		/// </summary>
		internal GCHandle LastUsedHandle {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _lastUsedHandle;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _lastUsedHandle = value;
		}

		internal List<nint> StackAlloceds {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _stackAlloceds;
		}

		internal bool IsConstrainedValueType {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isConstrainedValueType;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _isConstrainedValueType = value;
		}

		/// <summary>
		/// Instruction offset of explicit branch target if not null
		/// </summary>
		public uint? NextILOffset {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _nextILOffset;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set => _nextILOffset = value;
		}

		/// <summary>
		/// Whether ret instruction was executed
		/// </summary>
		public bool IsReturned {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _isReturned;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			internal set => _isReturned = value;
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
					_localTypes[i] = DnlibHelpers.ResolveTypeSig(_method, localDefs[i].Type);
			}
			else {
				_localTypes = Array.Empty<TypeDesc>();
			}
		}

		internal void ResolveDynamicContext(nint[] arguments) {
			_stackBase = _context.AcquireStack();
			_stack = _stackBase + InterpreterContext.StackSize;
			_handles = _context.AcquireHandles();
			_stackAlloceds = _context.AcquireStackAlloceds();
			if (!(_method is null)) {
				_arguments = Interpreter.ConvertArguments(arguments, this);
				_locals = new InterpreterSlot[_localTypes.Length];
				_handles.Push(GCHandle.Alloc(_arguments, GCHandleType.Pinned));
				_handles.Push(GCHandle.Alloc(_locals, GCHandleType.Pinned));
			}
			_isDisposed = false;
		}

		internal void ReleaseDynamicContext() {
			if (_isDisposed)
				throw new InvalidOperationException();

			if (!(_method is null)) {
				Array.Clear(_arguments, 0, _arguments.Length);
				_arguments = null;
				Array.Clear(_locals, 0, _locals.Length);
				_locals = null;
				_context.ReleaseMethodContext(this);
			}
			_context.ReleaseStack(_stackBase);
			_stackBase = null;
			_stack = null;
			_context.ReleaseHandles(_handles);
			_handles = null;
			_lastUsedHandle = default;
			_context.ReleaseStackAlloceds(_stackAlloceds);
			_stackAlloceds = null;
			_isConstrainedValueType = false;
			_nextILOffset = null;
			_isReturned = false;
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
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I4 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushI8(long value) {
			ref var slot = ref *--Stack;
			slot.I8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I8 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushI(nint value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushByRef(nint value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.ByRef | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushR4(float value) {
			ref var slot = ref *--Stack;
			slot.R4 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R4 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void PushR8(double value) {
			ref var slot = ref *--Stack;
			slot.R8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R8 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(nint value, AnnotatedElementType annotatedElementType) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = annotatedElementType;
		}

		/// <summary>
		/// Pushes empty slot onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public ref InterpreterSlot Push() {
			ref var slot = ref *--Stack;
			slot = default;
			return ref slot;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void Push(nint value, ElementType elementType) {
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
#if DEBUG
			if (_stack < _stackBase || _stack >= _stackBase + InterpreterContext.StackSize)
				throw new OutOfMemoryException();
			return ref *_stack;
#else
			return ref *_stack;
#endif
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

		/// <summary>
		/// Peeks all value on stack from stack top to bottom
		/// </summary>
		/// <returns></returns>
		public IEnumerable<InterpreterSlot> PeekAll() {
			int stackSize = (int)InterpreterContext.StackSize * Unsafe.SizeOf<InterpreterSlot>();
			nint stackTop = GetStackBase() + stackSize;
			nint stack = GetStack();
			nint bytesLeft = stackTop - stack;
			int length = (int)bytesLeft / Unsafe.SizeOf<InterpreterSlot>();
			for (int i = 0; i < length; i++)
				yield return GetSlot(i);

			nint GetStackBase() {
				return (nint)_stackBase;
			}

			nint GetStack() {
				return (nint)_stack;
			}

			InterpreterSlot GetSlot(int index) {
				return _stack[index];
			}
		}
		#endregion

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed)
				ReleaseDynamicContext();
		}
	}
}
