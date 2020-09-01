using System;
using System.Collections.Generic;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter manager
	/// </summary>
	public sealed class InterpreterManager {
		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<Type, IInterpreter> _interpreters;
		private IInterpreter _defaultInterpreter;

		internal IEnumerable<IInterpreter> Interpreters => _interpreters.Values;

		/// <summary>
		/// Default interpreter
		/// </summary>
		public IInterpreter DefaultInterpreter {
			get => _defaultInterpreter;
			set => _defaultInterpreter = value;
		}

		internal InterpreterManager(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
			_interpreters = new Dictionary<Type, IInterpreter>();
		}

		/// <summary>
		/// Get the interpreter by type
		/// </summary>
		/// <typeparam name="TInterpreter"></typeparam>
		/// <returns></returns>
		public TInterpreter Get<TInterpreter>() where TInterpreter : IInterpreter {
			var type = typeof(TInterpreter);
			if (_interpreters.TryGetValue(type, out var interpreter))
				return (TInterpreter)interpreter;
			if (type == typeof(Emit.Interpreter))
				interpreter = new Emit.Interpreter(_executionEngine);
			else
				throw new NotSupportedException();
			_interpreters.Add(type, interpreter);
			return (TInterpreter)interpreter;
		}
	}
}
