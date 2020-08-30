namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Flow control
	/// </summary>
	public enum FlowControl {
		/// <summary>
		/// Return instruction
		/// </summary>
		Return,

		/// <summary>
		/// Unconditional branch
		/// </summary>
		Branch,

		/// <summary>
		/// Conditional branch
		/// </summary>
		CondBranch,

		/// <summary>
		/// Throw instruction
		/// </summary>
		Throw
	}
}
