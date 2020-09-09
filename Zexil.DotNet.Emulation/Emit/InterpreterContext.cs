using System;
using System.Collections.Generic;
using System.Linq;
using dnlib.DotNet;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation.Emit {
	/// <summary>
	/// CIL instruction interpreter context
	/// </summary>
	public sealed unsafe class InterpreterContext : IDisposable {
		/// <summary>
		/// Stack size
		/// </summary>
		public const uint StackSize = 0x400;

		/// <summary>
		/// Type stack size
		/// </summary>
		public const uint TypeStackSize = StackSize / 4;

		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<MethodDesc, Cache<InterpreterMethodContext>> _methodContexts = new Dictionary<MethodDesc, Cache<InterpreterMethodContext>>();
		private readonly Cache<IntPtr> _stacks = Cache<IntPtr>.Create();
		private readonly Cache<IntPtr> _typeStacks = Cache<IntPtr>.Create();
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine (exists if current instance is not created by user)
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		internal InterpreterContext(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		internal InterpreterMethodContext AcquireMethodContext(MethodDesc method, ModuleDef moduleDef) {
			if (!_methodContexts.TryGetValue(method, out var cache)) {
				cache = Cache<InterpreterMethodContext>.Create();
				_methodContexts.Add(method, cache);
			}
			if (cache.TryAcquire(out var methodContext))
				return methodContext;
			return new InterpreterMethodContext(this, method, moduleDef);
		}

		internal void ReleaseMethodContext(InterpreterMethodContext methodContext) {
			_methodContexts[methodContext.Method].Release(methodContext);
		}

		internal void* AcquireStack() {
			if (_stacks.TryAcquire(out var stack))
				return (void*)stack;
			return Pal.AllocMemory(StackSize, false);
		}

		internal void ReleaseStack(void* stack) {
			_stacks.Release((IntPtr)stack);
		}

		internal void* AcquireTypeStack() {
			if (_typeStacks.TryAcquire(out var stack))
				return (void*)stack;
			return Pal.AllocMemory(TypeStackSize, false);
		}

		internal void ReleaseTypeStack(void* stack) {
			_typeStacks.Release((IntPtr)stack);
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var methodContext in _methodContexts.Values.SelectMany(t => t.Values))
					methodContext.Dispose();
				_methodContexts.Clear();
				foreach (var stack in _stacks.Values)
					Pal.FreeMemory((void*)stack);
				_stacks.Clear();
				_isDisposed = true;
			}
		}

		private struct Cache<T> {
			private Stack<T> _values;
#if DEBUG
			private int _maxValues;
#endif

			public IEnumerable<T> Values => _values;

			public static Cache<T> Create() {
				var cache = new Cache<T> {
					_values = new Stack<T>()
				};
				return cache;
			}

			public bool TryAcquire(out T value) {
				if (_values.Count == 0) {
#if DEBUG
					_maxValues++;
#endif
					value = default;
					return false;
				}
				else {
					value = _values.Pop();
					return true;
				}
			}

			public void Release(T value) {
				_values.Push(value);
			}

			public void Clear() {
#if DEBUG
				if (_values.Count < _maxValues)
					throw new InvalidOperationException($"Contains {_maxValues - _values.Count} unreleased value");
				else if (_values.Count > _maxValues)
					throw new InvalidOperationException($"Contains {_values.Count - _maxValues } value that were incorrectly released");
#endif

				_values.Clear();
			}
		}
	}
}
