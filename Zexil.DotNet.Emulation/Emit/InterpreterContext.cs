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
		/// Maximum stack size
		/// </summary>
		public const int MaximumStackSize = 100;

		private readonly ExecutionEngine _executionEngine;
		private readonly Dictionary<(MethodDef MethodDef, MethodDesc Method), Cache<InterpreterMethodContext>> _methodContexts = new Dictionary<(MethodDef MethodDef, MethodDesc Method), Cache<InterpreterMethodContext>>();
		private readonly Cache<nint> _stacks = Cache<nint>.Create();
		private readonly Cache<Stack<GCHandle>> _handleLists = Cache<Stack<GCHandle>>.Create();
		private readonly Cache<List<nint>> _stackAllocedLists = Cache<List<nint>>.Create();
		private bool _isDisposed;

		/// <summary>
		/// Bound execution engine
		/// </summary>
		public ExecutionEngine ExecutionEngine => _executionEngine;

		internal InterpreterContext(ExecutionEngine executionEngine) {
			_executionEngine = executionEngine;
		}

		internal InterpreterMethodContext AcquireMethodContext(MethodDef methodDef, MethodDesc method) {
			if (!_methodContexts.TryGetValue((methodDef, method), out var cache)) {
				cache = Cache<InterpreterMethodContext>.Create();
				_methodContexts.Add((methodDef, method), cache);
			}
			if (cache.TryAcquire(out var methodContext))
				return methodContext;
			return new InterpreterMethodContext(this, methodDef, method);
		}

		internal void ReleaseMethodContext(InterpreterMethodContext methodContext) {
			_methodContexts[(methodContext.MethodDef, methodContext.Method)].Release(methodContext);
		}

		internal InterpreterSlot* AcquireStack() {
			if (_stacks.TryAcquire(out nint stack))
				return (InterpreterSlot*)stack;
			return (InterpreterSlot*)Pal.AllocMemory((uint)sizeof(InterpreterSlot) * MaximumStackSize, false);
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

		internal List<nint> AcquireStackAlloceds() {
			if (_stackAllocedLists.TryAcquire(out var stackAlloceds))
				return stackAlloceds;
			return new List<nint>();
		}

		internal void ReleaseStackAlloceds(List<nint> stackAlloceds) {
			foreach (nint stackAlloced in stackAlloceds)
				Marshal.FreeHGlobal(stackAlloced);
			stackAlloceds.Clear();
			_stackAllocedLists.Release(stackAlloceds);
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
