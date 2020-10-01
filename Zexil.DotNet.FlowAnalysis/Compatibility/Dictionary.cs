#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;

namespace System.Collections.Generic {
	internal static class Dictionary2 {
		public static bool Remove<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, [MaybeNullWhen(false)] out TValue value) where TKey : notnull {
			if (dictionary is null)
				throw new ArgumentNullException(nameof(dictionary));

			if (dictionary.TryGetValue(key, out value)) {
				dictionary.Remove(key);
				return true;
			}

			value = default!;
			return false;
		}
	}
}
#endif
