using System;
using System.Collections;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

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
		private readonly List<IntPtr> _stackBases = new List<IntPtr>();
		[ThreadStatic]
		private byte* _stackBase;
		// 8MB stack for each thread
		[ThreadStatic]
		private byte* _stack;
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine (exists if current instance is not created by user)
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Stack base pointer (default is 8MB size for each thread)
		/// </summary>
		public byte* StackBase {
			get {
				if (_stackBase == null) {
					_stackBase = (byte*)Pal.AllocMemory(DefaultStackSize, false);
					lock (((ICollection)_stackBases).SyncRoot)
						_stackBases.Add((IntPtr)_stackBase);
				}
				return _stackBase;
			}
		}

		/// <summary>
		/// Stack pointer (default is 8MB size for each thread)
		/// </summary>
		public byte* Stack {
			get {
				if (_stack == null)
					_stack = StackBase;
				return _stack;
			}
			set {
				byte* stackBase = StackBase;
				if (value < stackBase || value > stackBase + DefaultStackSize)
					throw new ExecutionEngineException(new ArgumentOutOfRangeException());

				_stack = value;
			}
		}

		internal InterpreterContext(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var stackBase in _stackBases)
					Pal.FreeMemory((void*)stackBase);
				_stackBases.Clear();
				_isDisposed = true;
			}
		}
	}

	/// <summary>
	/// CIL instruction interpreter method context
	/// </summary>
	public sealed class InterpreterMethodContext {
		private readonly MethodDef _method;
		private readonly object[] _arguments;

		/// <summary>
		/// Interpreted method
		/// </summary>
		public MethodDef Method => _method;

		/// <summary>
		/// Arguments
		/// </summary>
		public object[] Arguments => _arguments;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method"></param>
		public InterpreterMethodContext(MethodDef method) {
			_method = method ?? throw new ArgumentNullException(nameof(method));
			_arguments = new object[method.Parameters.Count];
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		public InterpreterMethodContext(MethodDef method, params object[] arguments) {
			if (method is null)
				throw new ArgumentNullException(nameof(method));
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));
			if (arguments.Length != method.Parameters.Count)
				throw new ArgumentOutOfRangeException(nameof(arguments), "Not matched argument count.");

			_method = method;
			_arguments = arguments;
		}
	}

	/// <summary>
	/// CIL instruction interpreter which provides methods to emulate method execution.
	/// TODO: support thread local storage
	/// </summary>
	public sealed partial class Interpreter : IInterpreter, IDisposable {
#pragma warning disable CA1032 // Implement standard exception constructors
		private sealed class WrappedException : Exception {
#pragma warning restore CA1032 // Implement standard exception constructors
			public WrappedException(Exception exception) : base(string.Empty, exception) {
			}
		}

		private readonly InterpreterContext _context;
		private bool _isDisposed;

		/// <summary>
		/// Interpreter context
		/// </summary>
		public InterpreterContext Context => _context;

		internal Interpreter(ExecutionEngine executionEngine) {
			_context = new InterpreterContext(executionEngine);
		}

		/// <summary>
		/// Interprets a CIL instruction
		/// </summary>
		/// <param name="instruction"></param>
		/// <returns></returns>
		public Exception Interpret(Instruction instruction) {
			if (instruction is null)
				throw new ArgumentNullException(nameof(instruction));

			try {
				InterpretImpl(instruction, null);
			}
			catch (WrappedException ex) {
				return ex.InnerException;
			}
			return null;
		}

		/// <summary>
		/// Interprets a CIL instruction with specified method context
		/// </summary>
		/// <param name="instruction"></param>
		/// <param name="methodContext"></param>
		/// <returns></returns>
		public Exception Interpret(Instruction instruction, InterpreterMethodContext methodContext) {
			if (instruction is null)
				throw new ArgumentNullException(nameof(instruction));
			if (methodContext is null)
				throw new ArgumentNullException(nameof(methodContext));

			try {
				InterpretImpl(instruction, null);
			}
			catch (WrappedException ex) {
				return ex.InnerException;
			}
			return null;
		}

		object IInterpreter.InterpretFromStub(MethodDesc method, object[] arguments) {
			throw new NotImplementedException();
		}

		/// <inheritdoc/>
		public void Dispose() {
			if (!_isDisposed) {
				_context.Dispose();
				_isDisposed = true;
			}
		}
	}
}
