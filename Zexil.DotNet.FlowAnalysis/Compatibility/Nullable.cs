#if NETSTANDARD2_0
namespace System.Diagnostics.CodeAnalysis {
	[AttributeUsage(AttributeTargets.Method, Inherited = false)]
	internal sealed class DoesNotReturnAttribute : Attribute {
		public DoesNotReturnAttribute() {
		}
	}

	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class DoesNotReturnIfAttribute : Attribute {
		public bool ParameterValue { get; }

		public DoesNotReturnIfAttribute(bool parameterValue) {
			ParameterValue = parameterValue;
		}
	}
	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class MaybeNullWhenAttribute : Attribute {
		public bool ReturnValue { get; }

		public MaybeNullWhenAttribute(bool returnValue) {
			ReturnValue = returnValue;
		}
	}

	[AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
	internal sealed class NotNullWhenAttribute : Attribute {
		public bool ReturnValue { get; }

		public NotNullWhenAttribute(bool returnValue) {
			ReturnValue = returnValue;
		}
	}
}
#endif
