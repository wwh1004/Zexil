using System;
using System.Collections.Generic;
using System.Linq;
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
		private readonly MethodDef _methodDef;
		private readonly MethodDesc _method;
		private readonly TypeDesc[] _argumentTypes;
		private readonly TypeDesc[] _localTypes;

		/// <summary>
		/// Interpreter context
		/// </summary>
		public InterpreterContext Context => _context;

		/// <summary>
		/// Related <see cref="dnlib.DotNet.MethodDef"/>
		/// </summary>
		public MethodDef MethodDef => _methodDef;

		/// <summary>
		/// Interpreted method
		/// </summary>
		public MethodDesc Method => _method;

		/// <summary>
		/// Interpreted module (has no value if <see cref="Method"/> is <see langword="null"/>)
		/// </summary>
		public ModuleDesc Module => _method?.Module;

		/// <summary>
		/// Type generic arguments (has no value if <see cref="Method"/> is <see langword="null"/>)
		/// </summary>
		public TypeDesc[] TypeInstantiation => _method?.DeclaringType.Instantiation;

		/// <summary>
		/// Method generic arguments (has no value if <see cref="Method"/> is <see langword="null"/>)
		/// </summary>
		public TypeDesc[] MethodInstantiation => _method?.Instantiation;

		/// <summary>
		/// Argument types (element in array might be null if <see cref="Method"/> is <see langword="null"/>)
		/// </summary>
		public TypeDesc[] ArgumentTypes => _argumentTypes;

		/// <summary>
		/// Local variable types (element in array might be null if <see cref="Method"/> is <see langword="null"/>)
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
		public InterpreterSlot[] Arguments => _arguments;

		/// <summary>
		/// Local variables
		/// </summary>
		public InterpreterSlot[] Locals => _locals;

		/// <summary>
		/// Return buffer (pointer to return value)
		/// </summary>
		public ref InterpreterSlot ReturnBuffer {
			get {
				if (!_method.HasReturnType)
					throw new InvalidOperationException();

				return ref _arguments[_arguments.Length - 1];
			}
		}

		/// <summary>
		/// Stack base
		/// </summary>
		public InterpreterSlot* StackBase => _stackBase;

		/// <summary>
		/// Stack top
		/// </summary>
		public InterpreterSlot* Stack {
			get => _stack;
			set {
#if DEBUG
				if (value < _stackBase || value > _stackBase + InterpreterContext.MaximumStackSize)
					throw new OutOfMemoryException();
#endif
				_stack = value;
			}
		}

		/// <summary>
		/// Stack size
		/// </summary>
		public int StackSize => (int)(_stackBase + InterpreterContext.MaximumStackSize - _stack);

		/// <summary>
		/// Handles of objects
		/// </summary>
		internal Stack<GCHandle> Handles => _handles;

		/// <summary>
		/// Last used object handle
		/// </summary>
		internal GCHandle LastUsedHandle {
			get => _lastUsedHandle;
			set => _lastUsedHandle = value;
		}

		internal List<nint> StackAlloceds => _stackAlloceds;

		internal bool IsConstrainedValueType {
			get => _isConstrainedValueType;
			set => _isConstrainedValueType = value;
		}

		/// <summary>
		/// Instruction offset of explicit branch target if not null
		/// </summary>
		public uint? NextILOffset {
			get => _nextILOffset;
			set => _nextILOffset = value;
		}

		/// <summary>
		/// Whether ret instruction was executed
		/// </summary>
		public bool IsReturned {
			get => _isReturned;
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

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context"></param>
		/// <param name="methodDef"></param>
		/// <param name="method">Optional parameter</param>
		internal InterpreterMethodContext(InterpreterContext context, MethodDef methodDef, MethodDesc method) {
#if DEBUG
			System.Diagnostics.Debug.Assert(!(methodDef is null));
#endif
			_context = context;
			_methodDef = methodDef;
			_method = method;
			if (method is null) {
				_argumentTypes = methodDef.Parameters.Select(t => DnlibHelpers.TryResolveTypeSig(context.ExecutionEngine, method, t.Type)).ToArray();
			}
			else {
				_argumentTypes = method.Parameters;
			}
#if DEBUG
			for (int i = 0; i < methodDef.Body.Variables.Count; i++)
				System.Diagnostics.Debug.Assert(methodDef.Body.Variables[i].Index == i);
#endif
			_localTypes = methodDef.Body.Variables.Select(t => DnlibHelpers.TryResolveTypeSig(context.ExecutionEngine, method, t.Type)).ToArray();
		}

		internal void ResolveDynamicContext(nint[] arguments) {
			_stackBase = _context.AcquireStack();
			_stack = _stackBase + InterpreterContext.MaximumStackSize;
			_handles = _context.AcquireHandles();
			_stackAlloceds = _context.AcquireStackAlloceds();
			if (!(_argumentTypes is null) && !(arguments is null)) {
				_arguments = Interpreter.ConvertArguments(arguments, this);
				_handles.Push(GCHandle.Alloc(_arguments, GCHandleType.Pinned));
			}
			_locals = new InterpreterSlot[_localTypes.Length];
			_handles.Push(GCHandle.Alloc(_locals, GCHandleType.Pinned));
			_isDisposed = false;
		}

		internal void ReleaseDynamicContext() {
			if (_isDisposed)
				throw new InvalidOperationException();

			if (!(_arguments is null)) {
				Array.Clear(_arguments, 0, _arguments.Length);
				_arguments = null;
			}
			Array.Clear(_locals, 0, _locals.Length);
			_locals = null;
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
			if (!(_method is null))
				_context.ReleaseMethodContext(this);
		}

		#region public stack apis
		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushI4(int value) {
			ref var slot = ref *--Stack;
			slot.I4 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I4 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushI8(long value) {
			ref var slot = ref *--Stack;
			slot.I8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I8 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushI(nint value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushByRef(nint value) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.ByRef | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushR4(float value) {
			ref var slot = ref *--Stack;
			slot.R4 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R4 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void PushR8(double value) {
			ref var slot = ref *--Stack;
			slot.R8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R8 | AnnotatedElementType.Unmanaged;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void Push(nint value, AnnotatedElementType annotatedElementType) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.AnnotatedElementType = annotatedElementType;
		}

		/// <summary>
		/// Pushes empty slot onto stack top
		/// </summary>
		/// <returns></returns>
		public ref InterpreterSlot Push() {
			ref var slot = ref *--Stack;
			slot = default;
			return ref slot;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void Push(nint value, ElementType elementType) {
			ref var slot = ref *--Stack;
			slot.I = value;
			slot.ElementType = elementType;
		}

		/// <summary>
		/// Pushes value onto stack top
		/// </summary>
		/// <returns></returns>
		public void Push(in InterpreterSlot value) {
			*--Stack = value;
		}

		/// <summary>
		/// Pops value from stack top
		/// </summary>
		/// <returns></returns>
		public ref InterpreterSlot Pop() {
			return ref *Stack++;
		}

		/// <summary>
		/// Peeks value on stack top
		/// </summary>
		/// <returns></returns>
		public ref InterpreterSlot Peek() {
#if DEBUG
			if (_stack < _stackBase || _stack >= _stackBase + InterpreterContext.MaximumStackSize)
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
		public ref InterpreterSlot Peek(int index) {
#if DEBUG
			var stack = _stack + index;
			if (stack < _stackBase || stack >= _stackBase + InterpreterContext.MaximumStackSize)
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
			int length = StackSize;
			for (int i = 0; i < length; i++)
				yield return GetSlot(i);

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
