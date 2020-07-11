using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Represents a block context
	/// </summary>
	public interface IBlockContext {
	}

	/// <summary>
	/// A collection of block contexts
	/// </summary>
	public sealed class BlockContexts {
		private readonly Dictionary<object, IBlockContext> _contexts;

		internal Dictionary<object, IBlockContext> InternalContexts => _contexts;

		internal BlockContexts() {
			_contexts = new Dictionary<object, IBlockContext>();
		}

		/// <summary>
		/// Get the context by specified key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T Get<T>(object key) where T : class, IBlockContext {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (_contexts.Count == 0)
				throw new InvalidOperationException($"{nameof(_contexts)} is empty");

			var value = _contexts[key];
			if (!(value is T t))
				throw new InvalidCastException($"Real type of current context is {value.GetType()} and it can't be convert into {typeof(T)}");
			return t;
		}

		/// <summary>
		/// Get the context by specified key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="context"></param>
		/// <returns></returns>
		public bool TryGet<T>(object key, [NotNullWhen(true)] out T? context) where T : class, IBlockContext {
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			if (_contexts.Count != 0 && _contexts.TryGetValue(key, out var value)) {
				if (!(value is T t))
					throw new InvalidCastException($"Real type of current context is {value.GetType()} and it can't be convert into {typeof(T)}");
				context = t;
				return true;
			}
			else {
				context = null;
				return false;
			}
		}

		/// <summary>
		/// Set the context by specified key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <param name="context">A block context</param>
		/// <returns></returns>
		public T Set<T>(object key, T context) where T : class, IBlockContext {
			if (key is null)
				throw new ArgumentNullException(nameof(key));
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			_contexts[key] = context;
			return context;
		}

		/// <summary>
		/// Remove the context by specified key
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="key"></param>
		/// <returns></returns>
		public T Remove<T>(object key) where T : class, IBlockContext {
			if (key is null)
				throw new ArgumentNullException(nameof(key));

			if (!_contexts.Remove(key, out var value))
				throw new InvalidOperationException("Can't remove the context by specific key");
			if (!(value is T t)) {
				_contexts[key] = value;
				throw new InvalidCastException($"Real type of current context is {value.GetType()} and it can't be convert into {typeof(T)}");
			}
			return t;
		}
	}
}
