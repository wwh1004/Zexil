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
		/// Flow control type mask
		/// </summary>
		TypeMask = 3,

		/// <summary>
		/// Branch instruction with multi targets (e.g. used for CIL instruction switch)
		/// </summary>
		Switch = 1 << 2,

		/// <summary>
		/// Branch instruction that leaves try block (e.g. used for CIL instruction leave and leave.s)
		/// </summary>
		Leave = 1 << 3,

		/// <summary>
		/// Indirect branch
		/// </summary>
		Indirect = 1 << 4,

		/// <summary>
		/// Flow control annotation mask
		/// </summary>
		AnnotationMask = ~3
	}
}
