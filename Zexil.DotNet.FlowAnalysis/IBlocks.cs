using System.Collections.Generic;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Represents a block
	/// </summary>
	public interface IBlock {
		/// <summary>
		/// Block type
		/// </summary>
		BlockType Type { get; }

		/// <summary>
		/// Block flags
		/// </summary>
		BlockFlags Flags { get; set; }

		/// <summary>
		/// Block contexts
		/// </summary>
		BlockContexts Contexts { get; }

		/// <summary>
		/// Returns scope and throws if null
		/// </summary>
		IScopeBlock Scope { get; }

		/// <summary>
		/// Returns scope of current block
		/// </summary>
		IScopeBlock? ScopeNoThrow { get; set; }
	}

	/// <summary>
	/// Basic block
	/// </summary>
	public interface IBasicBlock : IBlock {
		/// <summary>
		/// Returns <see langword="true"/> if current basic block is empty
		/// </summary>
		public bool IsEmpty { get; }

		/// <summary>
		/// Flow control
		/// </summary>
		FlowControl FlowType { get; set; }

		/// <summary>
		/// Flow annotation
		/// </summary>
		FlowControl FlowAnnotation { get; set; }

		/// <summary>
		/// Flow control (type with annotation)
		/// </summary>
		FlowControl FlowControl { get; set; }

		/// <summary>
		/// Returns fall through and throws if null
		/// </summary>
		IBasicBlock FallThrough { get; }

		/// <summary>
		/// Returns fall through of current basic block
		/// </summary>
		IBasicBlock? FallThroughNoThrow { get; set; }

		/// <summary>
		/// Returns conditional target and throws if null
		/// </summary>
		IBasicBlock CondTarget { get; }

		/// <summary>
		/// Returns the conditional branch of current basic block (jumps into it if condition is true)
		/// </summary>
		IBasicBlock? CondTargetNoThrow { get; set; }

		/// <summary>
		/// Returns switch targets and throws if null
		/// </summary>
		ITargetList SwitchTargets { get; }

		/// <summary>
		/// Returns switch targets of current basic block
		/// </summary>
		ITargetList? SwitchTargetsNoThrow { get; set; }

		/// <summary>
		/// Returns predecessors of current basic block
		/// </summary>
		IDictionary<IBasicBlock, int> Predecessors { get; }

		/// <summary>
		/// Returns successors of current basic block
		/// </summary>
		IDictionary<IBasicBlock, int> Successors { get; }
	}

	/// <summary>
	/// Scope block
	/// </summary>
	public interface IScopeBlock : IBlock {
		/// <summary>
		/// Child blocks
		/// </summary>
		IList<IBlock> Blocks { get; }

		/// <summary>
		/// First block in current scope block
		/// </summary>
		IBlock FirstBlock { get; }

		/// <summary>
		/// Last block in current scope block
		/// </summary>
		IBlock LastBlock { get; }
	}

	/// <summary>
	/// Handler block
	/// </summary>
	public interface IHandlerBlock : IScopeBlock {
		/// <summary>
		/// The catch type if <see cref="IBlock.Type"/> is <see cref="BlockType.Catch"/>
		/// </summary>
		public object? CatchType { get; set; }
	}
}
