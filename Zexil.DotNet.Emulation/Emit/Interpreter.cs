using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// CIL instruction interpreter which provides methods to emulate method execution.
	/// TODO: support thread local storage
	/// </summary>
	public sealed partial class Interpreter : IInterpreter, IDisposable {
		private readonly InterpreterContext _context;
		private readonly TypeDesc _typeDescOfRuntimeTypeHandle;
		private readonly TypeDesc _typeDescOfRuntimeMethodHandle;
		private readonly TypeDesc _typeDescOfRuntimeFieldHandle;
		private Func<MethodDesc, MethodDef> _resolveMethodDef;
		private InterpretFromStubHandler _interpretFromStubUser;
		private bool _isDisposed;

		/// <summary>
		/// Interpreter context
		/// </summary>
		public InterpreterContext Context => _context;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _context.ExecutionEngine;

		/// <summary>
		/// Resolves a <see cref="MethodDef"/> instance by <see cref="MethodDesc"/>
		/// </summary>
		public Func<MethodDesc, MethodDef> ResolveMethodDef {
			get => _resolveMethodDef;
			set => _resolveMethodDef = value;
		}

		/// <inheritdoc />
		public InterpretFromStubHandler InterpretFromStubUser {
			get => _interpretFromStubUser;
			set => _interpretFromStubUser = value;
		}

		internal Interpreter(ExecutionEngine executionEngine) {
			_context = new InterpreterContext(executionEngine);
			_typeDescOfRuntimeTypeHandle = executionEngine.ResolveType(typeof(RuntimeTypeHandle));
			_typeDescOfRuntimeMethodHandle = executionEngine.ResolveType(typeof(RuntimeMethodHandle));
			_typeDescOfRuntimeFieldHandle = executionEngine.ResolveType(typeof(RuntimeFieldHandle));
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
		/// Create method context with specified <see cref="MethodDef"/>
		/// </summary>
		/// <param name="methodDef"></param>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext(MethodDef methodDef) {
			return CreateMethodContext(methodDef, null, null);
		}

		/// <summary>
		/// Create method context with specified <see cref="MethodDef"/> and <see cref="MethodDesc"/>
		/// </summary>
		/// <param name="methodDef"></param>
		/// <param name="method"></param>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext(MethodDef methodDef, MethodDesc method) {
			return CreateMethodContext(methodDef, method, null);
		}

		/// <summary>
		/// Create method context with specified <see cref="MethodDef"/>, <see cref="MethodDesc"/> and <paramref name="arguments"/>
		/// </summary>
		/// <param name="methodDef"></param>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		public InterpreterMethodContext CreateMethodContext(MethodDef methodDef, MethodDesc method, nint[] arguments) {
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));

			var methodContext = _context.AcquireMethodContext(methodDef, method);
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
		public void InterpretFromStub(MethodDesc method, nint[] arguments) {
			var resolveMethodDef = _resolveMethodDef;
			if (resolveMethodDef is null)
				throw new InvalidOperationException($"{nameof(ResolveMethodDef)} is null");

			var methodDef = resolveMethodDef(method);
			if (methodDef is null)
				throw new InvalidOperationException($"Resolving {nameof(MethodDef)} fails");

			var instructions = methodDef.Body.Instructions;
			using var methodContext = CreateMethodContext(methodDef, method, arguments);
			int index = 0;
			do {
				InterpretImpl(instructions[index], methodContext);
				if (methodContext.NextILOffset is uint nextILOffset) {
					index = FindInstructionIndex(instructions, nextILOffset);
					methodContext.NextILOffset = null;
				}
				else {
					index++;
				}
			} while (!methodContext.IsReturned);
		}

		/// <summary>
		/// Finds index of instruction by <paramref name="offset"/>
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public static int FindInstructionIndex(IList<Instruction> instructions, uint offset) {
			int lo = 0;
			int hi = 0 + instructions.Count - 1;
			while (lo <= hi) {
				int i = lo + ((hi - lo) >> 1);
				int order = (int)instructions[i].Offset - (int)offset;
				if (order == 0)
					return i;
				if (order < 0)
					lo = i + 1;
				else
					hi = i - 1;
			}
			throw new InvalidOperationException("Offsets of instructions are incorrect or branch target is not in instruction list");
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
