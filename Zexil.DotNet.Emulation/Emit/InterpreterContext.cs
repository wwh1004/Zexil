using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
		public const uint StackSize = 100;

		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<MethodDesc, Cache<InterpreterMethodContext>> _methodContexts = new Dictionary<MethodDesc, Cache<InterpreterMethodContext>>();
		private readonly Cache<nint> _stacks = Cache<nint>.Create();
		private readonly Cache<Stack<GCHandle>> _handleLists = Cache<Stack<GCHandle>>.Create();
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine
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

		internal InterpreterSlot* AcquireStack() {
			if (_stacks.TryAcquire(out nint stack))
				return (InterpreterSlot*)stack;
			return (InterpreterSlot*)Pal.AllocMemory((uint)sizeof(InterpreterSlot) * StackSize, false);
		}

		internal void ReleaseStack(InterpreterSlot* stack) {
			_stacks.Release((nint)stack);
		}

		internal Stack<GCHandle> AcquireHandles() {
			if (_handleLists.TryAcquire(out var handles))
				return handles;
			return new Stack<GCHandle>();
		}

		internal void ReleaseHandles(Stack<GCHandle> handles) {
			foreach (var handle in handles)
				handle.Free();
			handles.Clear();
			_handleLists.Release(handles);
		}

		/// <inheritdoc />
		public void Dispose() {
			if (!_isDisposed) {
				foreach (var methodContext in _methodContexts.Values.SelectMany(t => t.Values))
					methodContext.Dispose();
				_methodContexts.Clear();
				foreach (nint stack in _stacks.Values)
					Pal.FreeMemory(stack);
				_stacks.Clear();
				foreach (var handle in _handleLists.Values.SelectMany(t => t))
					handle.Free();
				_handleLists.Clear();
				_isDisposed = true;
			}
		}

		private struct Cache<T> {
			private Stack<T> _values;
#if DEBUG
			private object _maxValues;
#endif

			public IEnumerable<T> Values => _values;

			public static Cache<T> Create() {
				var cache = new Cache<T> {
					_values = new Stack<T>()
				};
#if DEBUG
				cache._maxValues = 0;
#endif
				return cache;
			}

			public bool TryAcquire(out T value) {
				if (_values.Count == 0) {
#if DEBUG
					System.Runtime.CompilerServices.Unsafe.Unbox<int>(_maxValues)++;
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
				if (_values.Count < (int)_maxValues)
					throw new InvalidOperationException($"Contains {(int)_maxValues - _values.Count} unreleased value");
				else if (_values.Count > (int)_maxValues)
					throw new InvalidOperationException($"Contains {_values.Count - (int)_maxValues} value that were incorrectly released");
#endif

				_values.Clear();
			}
		}
	}
}
