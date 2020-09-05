using System;
using System.Collections.Generic;
using System.Threading;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter stub for callback and more
	/// </summary>
	public static class InterpreterStub {
		private static readonly Dictionary<int, WeakReference<ModuleDesc>> _modules = new Dictionary<int, WeakReference<ModuleDesc>>();
		private static int _moduleId;

		/// <summary>
		/// Allocates module id for stub dispatching
		/// </summary>
		/// <returns></returns>
		public static int AllocateModuleId() {
			return Interlocked.Increment(ref _moduleId) - 1;
		}

		/// <summary>
		/// Register a module in interpret stub
		/// </summary>
		/// <param name="module"></param>
		/// <param name="moduleId"></param>
		public static void RegisterModule(ModuleDesc module, int moduleId) {
			_modules.Add(moduleId, new WeakReference<ModuleDesc>(module));
		}

		/// <summary>
		/// Dispatches calling to default <see cref="IInterpreter"/> of corresponding <see cref="ExecutionEngine"/>
		/// </summary>
		/// <param name="moduleId"></param>
		/// <param name="methodToken"></param>
		/// <param name="arguments"></param>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		public static object Dispatch(int moduleId, int methodToken, object[] arguments, Type[] typeInstantiation, Type[] methodInstantiation) {
			if (!_modules.TryGetValue(moduleId, out var moduleWeakRef))
				throw new ExecutionEngineException(new InvalidOperationException());
			if (!moduleWeakRef.TryGetTarget(out var module))
				throw new ExecutionEngineException(new ObjectDisposedException(nameof(ExecutionEngine)));
			var interpreter = module.ExecutionEngine.InterpreterManager.DefaultInterpreter;
			if (interpreter is null)
				throw new ExecutionEngineException(new InvalidOperationException("Default interpreter isn't set."));

			var method = module.ResolveMethod(methodToken);
			if (!(typeInstantiation is null) || !(methodInstantiation is null))
				method = method.Instantiate(typeInstantiation, methodInstantiation);
			var interpretFromStubUser = interpreter.InterpretFromStubUser;
			return !(interpretFromStubUser is null) ? interpretFromStubUser(method, arguments) : interpreter.InterpretFromStub(method, arguments);
		}
	}
}

