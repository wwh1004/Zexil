using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
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
		private Func<ModuleDesc, ModuleDef> _resolveModuleDef;
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
		/// Resolves a <see cref="ModuleDef"/> instance by <see cref="ModuleDesc"/>
		/// </summary>
		public Func<ModuleDesc, ModuleDef> ResolveModuleDef {
			get => _resolveModuleDef;
			set => _resolveModuleDef = value;
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public InterpreterMethodContext CreateMethodContext(ModuleDef moduleDef, MethodDesc method, params nint[] arguments) {
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
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public InterpreterMethodContext CreateMethodContext(ModuleDef moduleDef, MethodDef methodDef, params nint[] arguments) {
			if (moduleDef is null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));
			if (methodDef.HasGenericParameters || methodDef.DeclaringType.HasGenericParameters)
				throw new NotSupportedException($"Creating method context by {nameof(MethodDef)} does NOT support generic method or method in generic type.");
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

			var module = ExecutionEngine.Context.Modules.FirstOrDefault(t => t.ScopeName == moduleDef.ScopeName && t.Assembly.FullName == moduleDef.Assembly.FullName);
			if (module is null)
				throw new InvalidOperationException("Specified module isn't loaded.");
			var method = module.ResolveMethod(methodDef.MDToken.ToInt32());
			var methodContext = _context.AcquireMethodContext(method, moduleDef);
			methodContext.ResolveDynamicContext(arguments);
			return methodContext;
		}

		/// <summary>
		/// Create method context with specified <see cref="MethodDesc"/>
		/// </summary>
		/// <param name="moduleDef"></param>
		/// <param name="method"></param>
		/// <param name="methodDef"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public InterpreterMethodContext CreateMethodContext(ModuleDef moduleDef, MethodDesc method, MethodDef methodDef, params nint[] arguments) {
			if (moduleDef is null)
				throw new ArgumentNullException(nameof(moduleDef));
			if (method is null)
				throw new ArgumentNullException(nameof(method));
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));
			if (methodDef.HasGenericParameters || methodDef.DeclaringType.HasGenericParameters)
				throw new NotSupportedException($"Creating method context by {nameof(MethodDef)} does NOT support generic method or method in generic type.");
			if (arguments is null)
				throw new ArgumentNullException(nameof(arguments));

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
		public void InterpretFromStub(MethodDesc method, nint[] arguments) {
			var resolveModuleDef = _resolveModuleDef;
			if (resolveModuleDef is null)
				throw new InvalidOperationException($"{nameof(ResolveModuleDef)} is null");

			var moduleDef = resolveModuleDef(method.Module);
			if (moduleDef is null)
				throw new InvalidOperationException($"Resolving {nameof(ModuleDef)} fails");

			var methodDef = (MethodDef)moduleDef.ResolveToken(method.MetadataToken);
			var instructions = methodDef.Body.Instructions;
			using var methodContext = CreateMethodContext(moduleDef, method, methodDef, arguments);
			for (int i = 0; i < instructions.Count; i++) {
			loop:
				InterpretImpl(instructions[i], methodContext);
				if (methodContext.IsReturned)
					break;
				if (!(methodContext.NextILOffset is uint nextILOffset))
					continue;

				i = FindNextIndex(instructions, nextILOffset);
				methodContext.NextILOffset = null;
				goto loop;
			}
		}

		/// <summary>
		/// Finds index of next instruction by <paramref name="nextILOffset"/>
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="nextILOffset"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int FindNextIndex(IList<Instruction> instructions, uint nextILOffset) {
			int lo = 0;
			int hi = 0 + instructions.Count - 1;
			while (lo <= hi) {
				int i = lo + ((hi - lo) >> 1);
				int order = (int)instructions[i].Offset - (int)nextILOffset;
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
