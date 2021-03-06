using System;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block type
	/// </summary>
	public enum BlockType {
		/// <summary>
		/// Invalid value
		/// </summary>
		None,

		/// <summary>
		/// Basic block
		/// </summary>
		Basic,

		/// <summary>
		/// Protected block (including try, filter, catch, finally, fault)
		/// </summary>
		Protected,

		/// <summary>
		/// Try block
		/// </summary>
		Try,

		/// <summary>
		/// Filter block
		/// </summary>
		Filter,

		/// <summary>
		/// Catch block
		/// </summary>
		Catch,

		/// <summary>
		/// Finally block
		/// </summary>
		Finally,

		/// <summary>
		/// Fault block
		/// </summary>
		Fault,

		/// <summary>
		/// Method block
		/// </summary>
		Method
	}

	/// <summary>
	/// Block flags
	/// </summary>
	[Flags]
	public enum BlockFlags {
		/// <summary>
		/// None
		/// Target(s): <see cref="IBlock"/>
		/// </summary>
		None = 0,

		/// <summary>
		/// Generated by code
		/// Target(s): <see cref="IBlock"/>
		/// </summary>
		Generated = 1 << 0,

		/// <summary>
		/// Prevents to be inlined
		/// Target(s): <see cref="IBlock"/>
		/// </summary>
		NoInlining = 1 << 1,

		/// <summary>
		/// Trampoline, used for a scope block that has multiple entries (e.g. VB.NET On Error Resume Next will cause branch into try block)
		/// Target(s): <see cref="IBasicBlock"/>
		/// </summary>
		Trampoline = 1 << 2,

#if DEBUG
		/// <summary>
		/// Block is erased so this block should NOT be used again
		/// Target(s): <see cref="IBlock"/>
		/// </summary>
		Erased = 1 << 32
#endif
	}

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
