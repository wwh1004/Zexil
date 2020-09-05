using System;
using System.Reflection;

namespace Zexil.DotNet.Emulation {
	internal enum CLRFlavor {
		Framework,
		Core
	}

	internal static class CLREnvironment {
		public static readonly CLRFlavor Flavor = GetCLRFlavor();
		public static readonly Version Version = Environment.Version;
		public static readonly bool IsFramework2x = Flavor == CLRFlavor.Framework && Version.Major == 2;

		private static CLRFlavor GetCLRFlavor() {
			var assemblyProductAttribute = typeof(object).Assembly.GetCustomAttribute<AssemblyProductAttribute>();
			string product = assemblyProductAttribute.Product;
			if (product.EndsWith("Framework", StringComparison.Ordinal))
				return CLRFlavor.Framework;
			else if (product.EndsWith("Core", StringComparison.Ordinal))
				return CLRFlavor.Core;
			else
				throw new NotSupportedException();
		}
	}
}
