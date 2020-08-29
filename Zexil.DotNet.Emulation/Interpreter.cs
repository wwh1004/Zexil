using System;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// CIL instruction interpreter context
	/// </summary>
	public sealed unsafe class InterpreterContext {

	}

	/// <summary>
	/// CIL instruction interpreter which provides methods to emulate method execution.
	/// </summary>
	public sealed class Interpreter {
#pragma warning disable CA1032 // Implement standard exception constructors
		private sealed class InterpreterWrapperException : Exception {
#pragma warning restore CA1032 // Implement standard exception constructors
			public InterpreterWrapperException(Exception exception) : base(string.Empty, exception) {
			}
		}

		private readonly ExecutionEngine _executionEngine;

		/// <summary>
		/// Bound execution engine (exists if current instance is not created by user)
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		/// <summary>
		/// Constructor
		/// </summary>
		public Interpreter() {
		}

		internal Interpreter(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		public Exception Interpret(Instruction instruction, InterpreterContext context) {
			return null;
		}

		public static InterpreterContext CreateContext(MethodDef methodDef, object[] arguments) {
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));


		}
	}
}
