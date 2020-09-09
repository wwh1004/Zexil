using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// CIL instruction interpreter context
	/// </summary>
	public sealed unsafe class InterpreterContext : IDisposable {
		/// <summary>
		/// Stack size
		/// </summary>
		public const uint StackSize = 0x400;

		/// <summary>
		/// Type stack size
		/// </summary>
		public const uint TypeStackSize = StackSize / 4;

		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<MethodDesc, Cache<InterpreterMethodContext>> _methodContexts = new Dictionary<MethodDesc, Cache<InterpreterMethodContext>>();
		private readonly Cache<IntPtr> _stacks = Cache<IntPtr>.Create();
		private readonly Cache<IntPtr> _typeStacks = Cache<IntPtr>.Create();
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine (exists if current instance is not created by user)
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		internal InterpreterContext(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		internal InterpreterMethodContext AcquireMethodContext(MethodDesc method, ModuleDef moduleDef) {
			if (!_methodContexts.TryGetValue(method, out var cache)) {
				cache = Cache<InterpreterMethodContext>.Create();
				_methodContexts.Add(method, cache);
			}
			if (cache.TryAcquire(out var methodContext))
				return methodContext;
			return new InterpreterMethodContext(this, method, moduleDef);
		}

		internal void ReleaseMethodContext(InterpreterMethodContext methodContext) {
			_methodContexts[methodContext.Method].Release(methodContext);
		}

		internal void* AcquireStack() {
			if (_stacks.TryAcquire(out var stack))
				return (void*)stack;
			return Pal.AllocMemory(StackSize, false);
		}

		internal void ReleaseStack(void* stack) {
			_stacks.Release((IntPtr)stack);
		}

		internal void* AcquireTypeStack() {
			if (_typeStacks.TryAcquire(out var stack))
				return (void*)stack;
			return Pal.AllocMemory(TypeStackSize, false);
		}

		internal void ReleaseTypeStack(void* stack) {
			_typeStacks.Release((IntPtr)stack);
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var methodContext in _methodContexts.Values.SelectMany(t => t.Values))
					methodContext.Dispose();
				_methodContexts.Clear();
				foreach (var stack in _stacks.Values)
					Pal.FreeMemory((void*)stack);
				_stacks.Clear();
				_isDisposed = true;
			}
		}

		private struct Cache<T> {
			private Stack<T> _values;
#if DEBUG
			private int _maxValues;
#endif

			public IEnumerable<T> Values => _values;

			public static Cache<T> Create() {
				var cache = new Cache<T> {
					_values = new Stack<T>()
				};
				return cache;
			}

			public bool TryAcquire(out T value) {
				if (_values.Count == 0) {
#if DEBUG
					_maxValues++;
#endif
					value = default;
					return false;
				}
				else {
					value = _values.Pop();
					return true;
				}
			}

			public void Release(T value) {
				_values.Push(value);
			}

			public void Clear() {
#if DEBUG
				if (_values.Count < _maxValues)
					throw new InvalidOperationException($"Contains {_maxValues - _values.Count} unreleased value");
				else if (_values.Count > _maxValues)
					throw new InvalidOperationException($"Contains {_values.Count - _maxValues } value that were incorrectly released");
#endif

				_values.Clear();
			}
		}
	}

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
		private byte* _stack;
		private byte* _currentStack;
		private ElementType* _typeStack;
		private ElementType* _currentTypeStack;
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
		/// Stack
		/// </summary>
		public byte* Stack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _stack;
		}

		/// <summary>
		/// Current stack
		/// </summary>
		public byte* CurrentStack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _currentStack;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
#if DEBUG
				if (value < _stack || value >= _stack + InterpreterContext.StackSize)
					throw new OutOfMemoryException();
#endif
				_currentStack = value;
			}
		}

		/// <summary>
		/// Type stack
		/// </summary>
		public ElementType* TypeStack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _typeStack;
		}

		/// <summary>
		/// Current type stack
		/// </summary>
		public ElementType* CurrentTypeStack {
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get => _currentTypeStack;
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			set {
#if DEBUG
				if (value < _typeStack || value >= _typeStack + InterpreterContext.TypeStackSize)
					throw new OutOfMemoryException();
#endif
				_currentTypeStack = value;
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
			if (!_isDisposed)
				throw new InvalidOperationException();

			if (!(_method is null)) {
				if (arguments is null)
					throw new ArgumentNullException(nameof(arguments));
				if (arguments.Length != _method.Parameters.Length + (_method.HasReturnType ? 1 : 0))
					throw new ArgumentException(nameof(arguments));

				_arguments = arguments;
				_locals = new void*[_localTypes.Length];
			}
			_stack = (byte*)_context.AcquireStack();
			_typeStack = (ElementType*)_context.AcquireTypeStack();
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
			_context.ReleaseStack(_stack);
			_context.ReleaseTypeStack(_typeStack);
			_stack = null;
			_currentStack = null;
			_typeStack = null;
			_currentTypeStack = null;
			_isDisposed = true;
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed)
				ReleaseDynamicContext();
		}
	}

	/// <summary>
	/// CIL instruction interpreter which provides methods to emulate method execution.
	/// TODO: support thread local storage
	/// </summary>
	public sealed unsafe partial class Interpreter : IInterpreter, IDisposable {
		private readonly InterpreterContext _context;
		private InterpretFromStubHandler _interpretFromStubUser;
		private bool _isDisposed;

		/// <summary>
		/// Interpreter context
		/// </summary>
		public InterpreterContext Context => _context;

		/// <inheritdoc />
		public InterpretFromStubHandler InterpretFromStubUser {
			get => _interpretFromStubUser;
			set => _interpretFromStubUser = value;
		}

		internal Interpreter(ExecutionEngine executionEngine) {
			_context = new InterpreterContext(executionEngine);
		}

		/// <summary>
		/// Creates method-irrelated context
		/// </summary>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext() {
			var methodContext = new InterpreterMethodContext(_context);
			methodContext.ResolveDynamicContext(null);
			return methodContext;
		}

		/// <summary>
		/// Create method context with specified <see cref="MethodDesc"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext(ModuleDef moduleDef, MethodDesc method, params void*[] arguments) {
			if (moduleDef is null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (method is null)
				throw new ArgumentNullException(nameof(method));
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

			var methodContext = _context.AcquireMethodContext(method, moduleDef);
			methodContext.ResolveDynamicContext(arguments);
			return methodContext;
		}

		/// <summary>
		/// Create method context with specified <see cref="MethodDesc"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <param name="methodDef"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext(ModuleDef moduleDef, MethodDef methodDef, params void*[] arguments) {
			if (moduleDef is null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));
			if (methodDef.HasGenericParameters || methodDef.DeclaringType.HasGenericParameters)
				throw new NotSupportedException($"Creating method context by {nameof(MethodDef)} does NOT support generic method or method in generic type.");
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

			var eeContext = _context.ExecutionEngine.Context;
			var module = eeContext.Modules.FirstOrDefault(t => t.ScopeName == moduleDef.ScopeName && t.Assembly.FullName == moduleDef.Assembly.FullName);
			if (module is null)
				throw new InvalidOperationException("Specified module isn't loaded.");
			var method = module.ResolveMethod(methodDef.MDToken.ToInt32());
			var methodContext = _context.AcquireMethodContext(method, moduleDef);
			methodContext.ResolveDynamicContext(arguments);
			return methodContext;
		}

		/// <summary>
		/// Interprets a CIL instruction with specified method context
		/// </summary>
		/// <param name="instruction"></param>
		/// <param name="methodContext"></param>
		public void Interpret(Instruction instruction, InterpreterMethodContext methodContext) {
			if (instruction is null)
				throw new ArgumentNullException(nameof(instruction));
			if (methodContext is null)
				throw new ArgumentNullException(nameof(methodContext));

			InterpretImpl(instruction, methodContext);
		}

		/// <inheritdoc />
		public void InterpretFromStub(MethodDesc method, void*[] arguments) {
			throw new NotImplementedException();
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				_context.Dispose();
				_isDisposed = true;
			}
		}
	}
}
