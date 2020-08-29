using System.Reflection;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Runtime assembly
	/// </summary>
	public sealed unsafe class AssemblyDesc {
		private readonly Assembly _assembly;
		private readonly void* _rawAssembly;

		/// <summary>
		/// Assembly
		/// </summary>
		public Assembly Assembly => _assembly;

		/// <summary>
		/// Original assembly data
		/// </summary>
		public void* RawAssembly => _rawAssembly;

		internal AssemblyDesc(Assembly assembly, void* rawAssembly) {
			_assembly = assembly;
			_rawAssembly = rawAssembly;
		}
	}
}
