using System;
using System.Linq;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.Emit {
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
