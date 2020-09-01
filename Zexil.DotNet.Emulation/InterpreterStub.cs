using System;
using System.Collections.Generic;
using System.Threading;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter stub for callback and more
	/// </summary>
	public static class InterpreterStub {
		private static readonly Dictionary<int, WeakReference<ModuleDesc>> _modules = new Dictionary<int, WeakReference<ModuleDesc>>();
		private static int _id;

		internal static void Register(ModuleDesc module) {
			int id = Interlocked.Increment(ref _id) - 1;
			_modules.Add(id, new WeakReference<ModuleDesc>(module));
		}

		/// <summary>
		/// Dispatches calling to default <see cref="IInterpreter"/> of corresponding <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="methodToken"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public static object Dispatch(int moduleId, int methodToken, object[] arguments) {
			if (arguments is null)
				throw new ExecutionEngineException(new ArgumentNullException(nameof(arguments)));
			if (!_modules.TryGetValue(moduleId, out var moduleWeakRef))
				throw new ExecutionEngineException(new InvalidOperationException());
			if (!moduleWeakRef.TryGetTarget(out var module))
				throw new ExecutionEngineException(new ObjectDisposedException(nameof(ExecutionEngine)));
			var interpreter = module.ExecutionEngine.InterpreterManager.DefaultInterpreter;
			if (interpreter is null)
				throw new ExecutionEngineException(new InvalidOperationException("Default interpreter isn't set."));

			var methodDesc = module.ResolveMethod(methodToken);
			return interpreter.InterpretFromStub(methodDesc, arguments);
		}
	}
}

