using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// CIL instruction interpreter context
	/// </summary>
	public sealed unsafe class InterpreterContext : IDisposable {
		/// <summary>
		/// Default stack size
		/// </summary>
		public const uint DefaultStackSize = 0x800000;

		private readonly ExecutionEngine _executionEngine;
		private readonly Stack<IntPtr> _stacks = new Stack<IntPtr>();
#if DEBUG
		private int _maxStack;
#endif
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine (exists if current instance is not created by user)
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		internal InterpreterContext(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		internal void* AcquireStack() {
			lock (((ICollection)_stacks).SyncRoot) {
				if (_stacks.Count == 0) {
#if DEBUG
					_maxStack++;
#endif
					void* stack = Pal.AllocMemory(0x400, false);
					_stacks.Push((IntPtr)stack);
					return stack;
				}
				else {
					return (void*)_stacks.Pop();
				}
			}
		}

		internal void ReleaseStack(void* stack) {
			lock (((ICollection)_stacks).SyncRoot)
				_stacks.Push((IntPtr)stack);
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
#if DEBUG
				if (_stacks.Count < _maxStack)
					throw new InvalidOperationException($"Contains {_maxStack - _stacks.Count} unfreed stack");
				else if (_stacks.Count > _maxStack)
					throw new InvalidOperationException($"Contains {_stacks.Count - _maxStack } memory block that were incorrectly freed");
#endif
				foreach (var stack in _stacks)
					Pal.FreeMemory((void*)stack);
				_stacks.Clear();
				_isDisposed = true;
			}
		}
	}

	/// <summary>
	/// CIL instruction interpreter method context
	/// </summary>
	public sealed unsafe class InterpreterMethodContext : IDisposable {
		private readonly MethodDesc _method;
		private readonly void*[] _arguments;
		private InterpreterContext _context;
		private byte* _stack;
		private MethodDef _methodDef;
		private bool _isDisposed;

		/// <summary>
		/// Interpreted method
		/// </summary>
		public MethodDesc Method => _method;

		/// <summary>
		/// Arguments
		/// 
		/// refType  -> pointer to clr class "Object"
		/// refType* -> secondary pointer to clr class "Object"
		/// valType  -> pointer to first instance field
		/// valType* -> pointer to first instance field
		/// genType  -> depend on it is a reference type or value type
		/// genType* -> depend on it is a reference type or value type
		/// </summary>
		public void*[] Arguments => _arguments;

		/// <summary>
		/// Type generic arguments
		/// </summary>
		public TypeDesc[] TypeInstantiation => _method.DeclaringType.Instantiation;

		/// <summary>
		/// Method generic arguments
		/// </summary>
		public TypeDesc[] MethodInstantiation => _method.Instantiation;

		internal byte* Stack => _stack;

		/// <summary>
		/// Related <see cref="dnlib.DotNet.MethodDef"/>
		/// </summary>
		public MethodDef MethodDef => _methodDef;

		internal InterpreterMethodContext() {
		}

		internal InterpreterMethodContext(MethodDesc method, params void*[] arguments) {
			_method = method ?? throw new ArgumentNullException(nameof(method));
			_arguments = arguments ?? throw new ArgumentNullException(nameof(arguments));
		}

		internal void ResolveContext(InterpreterContext context, ModuleDef moduleDef) {
			_context = context;
			_stack = (byte*)context.AcquireStack();
			if (_method is null)
				return;

			_methodDef = (MethodDef)moduleDef.ResolveToken(_method.MetadataToken);
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				_context.ReleaseStack(_stack);
				_isDisposed = true;
			}
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
			var methodContext = new InterpreterMethodContext();
			methodContext.ResolveContext(_context, null);
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

			var methodContext = new InterpreterMethodContext(method, arguments);
			methodContext.ResolveContext(_context, moduleDef);
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
			var methodContext = new InterpreterMethodContext(method, arguments);
			methodContext.ResolveContext(_context, moduleDef);
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
			//throw new NotImplementedException();
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
