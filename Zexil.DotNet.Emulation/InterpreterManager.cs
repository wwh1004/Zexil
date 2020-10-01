using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter manager (thread local and thread safe)
	/// </summary>
	public sealed class InterpreterManager {
		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<Type, ThreadLocal<IInterpreter>> _interpreters = new Dictionary<Type, ThreadLocal<IInterpreter>>();
		private Type _defaultInterpreterType;

		internal IEnumerable<ThreadLocal<IInterpreter>> Interpreters => _interpreters.Values;

		/// <summary>
		/// Default interpreter
		/// </summary>
		public IInterpreter DefaultInterpreter => !(_defaultInterpreterType is null) ? GetImpl(_defaultInterpreterType) : null;

		/// <summary>
		/// Default interpreter type
		/// </summary>
		public Type DefaultInterpreterType {
			get => _defaultInterpreterType;
			set {
				if (!(value is null) && !typeof(IInterpreter).IsAssignableFrom(value))
					throw new ArgumentOutOfRangeException(nameof(value));

				_defaultInterpreterType = value;
			}
		}

		internal InterpreterManager(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine ?? throw new ArgumentNullException(nameof(executionEngine));
		}

		/// <summary>
		/// Get the interpreter by type
		/// </summary>
		/// <typeparam name="TInterpreter"></typeparam>
		/// <returns></returns>
		public TInterpreter Get<TInterpreter>() where TInterpreter : IInterpreter {
			return (TInterpreter)GetImpl(typeof(TInterpreter));
		}

		/// <summary>
		/// Get the interpreter by type
		/// </summary>
		/// <param name="interpreterType"></param>
		/// <returns></returns>
		public IInterpreter Get(Type interpreterType) {
			if (interpreterType is null)
				throw new ArgumentNullException(nameof(interpreterType));
			if (!interpreterType.IsAssignableFrom(typeof(IInterpreter)))
				throw new ArgumentOutOfRangeException(nameof(interpreterType));

			return GetImpl(interpreterType);
		}

		private IInterpreter GetImpl(Type interpreterType) {
			const BindingFlags BINDING_FLAGS = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.CreateInstance;

			if (_interpreters.TryGetValue(interpreterType, out var interpreter))
				return interpreter.Value;
			interpreter = new ThreadLocal<IInterpreter>(() => (IInterpreter)Activator.CreateInstance(interpreterType, BINDING_FLAGS, null, new object[] { _executionEngine }, null), true);
			_interpreters.Add(interpreterType, interpreter);
			return interpreter.Value;
		}
	}
}
