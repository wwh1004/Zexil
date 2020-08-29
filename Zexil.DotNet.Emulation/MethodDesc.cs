using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime method
	/// </summary>
	public sealed class MethodDesc {
		private readonly MethodInfo _internalValue;

		/// <summary>
		/// Internal value
		/// </summary>
		public MethodInfo InternalValue => _internalValue;

#pragma warning disable IDE0060 // Remove unused parameter
		internal MethodDesc(ExecutionEngine runtime, MethodInfo method) {
#pragma warning restore IDE0060 // Remove unused parameter
			_internalValue = method;
		}

		/// <inheritdoc/>
		public override string ToString() {
			return _internalValue.ToString();
		}
	}
}
