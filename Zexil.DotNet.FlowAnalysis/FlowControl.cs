namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Flow control
	/// </summary>
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
		Throw
	}
}
