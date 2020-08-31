using System;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Flow control
	/// </summary>
	[Flags]
	public enum FlowControl {
		/// <summary>
		/// Unconditional branch
		/// </summary>
		Branch,

		/// <summary>
		/// Conditional branch
		/// </summary>
		CondBranch,

		/// <summary>
		/// Return instruction
		/// </summary>
		Return,

		/// <summary>
		/// Throw instruction
		/// </summary>
		Throw,

		/// <summary>
		/// Basic flow control mask
		/// </summary>
		BasicMask = Throw,

		
	}
}
