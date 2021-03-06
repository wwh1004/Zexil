using System;
using System.Collections.Generic;
using System.Diagnostics;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Zexil.DotNet.FlowAnalysis.Collections;
using DNE = dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	/// <summary>
	/// Represents a block
	/// </summary>
	public abstract class Block : IBlock {
		private BlockFlags _flags;
		private readonly BlockContexts _contexts;
		private ScopeBlock? _scope;

		/// <summary>
		/// Default constructor
		/// </summary>
		protected internal Block() {
			_contexts = new BlockContexts();
		}

		/// <inheritdoc />
		public abstract BlockType Type { get; }

		/// <inheritdoc />
		public BlockFlags Flags {
			get => _flags;
			set => _flags = value;
		}

		/// <inheritdoc />
		public BlockContexts Contexts => _contexts;

		/// <summary>
		/// Returns scope and throws if null
		/// </summary>
		public ScopeBlock Scope => ScopeNoThrow ?? throw new ArgumentNullException(nameof(ScopeNoThrow));

		/// <summary>
		/// Returns scope of current block
		/// </summary>
		public ScopeBlock? ScopeNoThrow {
			get => _scope;
			set => _scope = value;
		}

		/// <inheritdoc />
		public override string ToString() {
			return BlockFormatter.Format(this);
		}

		#region IBlock
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IScopeBlock IBlock.Scope => Scope;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IScopeBlock? IBlock.ScopeNoThrow { get => ScopeNoThrow; set => ScopeNoThrow = (ScopeBlock?)value; }
		#endregion
	}

	/// <summary>
	/// Basic block
	/// </summary>
	public sealed class BasicBlock : Block, IBasicBlock {
		private readonly List<Instruction> _instructions;
		private readonly Instruction _branchInstruction;
		private FlowControl _flowControl;
		private BasicBlock? _fallThrough;
		private BasicBlock? _condTarget;
		private TargetList? _switchTargets;
		private readonly BbRefDict<BasicBlock> _predecessors;
		private readonly BbRefDict<BasicBlock> _successors;

		/// <inheritdoc />
		public override BlockType Type => BlockType.Basic;

		/// <summary>
		/// Instructions in current basic block excluding branch instruction
		/// </summary>
		public IList<Instruction> Instructions => _instructions;

		/// <summary>
		/// Returns <see langword="true"/> if <see cref="Instructions"/> is empty
		/// </summary>
		public bool IsEmpty => _instructions.Count == 0;

		/// <summary>
		/// Returns branch instruction of current basic block (dummy instruction)
		/// </summary>
		public Instruction BranchInstruction => _branchInstruction;

		/// <summary>
		/// Returns branch opcode of current basic block
		/// </summary>
		public OpCode BranchOpcode {
			get => _branchInstruction.OpCode;
			set {
				_branchInstruction.OpCode = value;
				_flowControl = value.FlowControl switch
				{
					DNE.FlowControl.Branch => FlowControl.Branch,
					DNE.FlowControl.Cond_Branch => FlowControl.CondBranch,
					DNE.FlowControl.Return => FlowControl.Return,
					DNE.FlowControl.Throw => FlowControl.Throw,
					_ => throw new ArgumentOutOfRangeException(nameof(value))
				} | value.Code switch
				{
					Code.Switch => FlowControl.Switch,
					Code.Leave => FlowControl.Leave,
					_ => 0
				};
			}
		}

		/// <inheritdoc />
		public FlowControl FlowType {
			get => _flowControl & FlowControl.TypeMask;
			set => _flowControl = (value & FlowControl.TypeMask) | (_flowControl & FlowControl.AnnotationMask);
		}

		/// <inheritdoc />
		public FlowControl FlowAnnotation {
			get => _flowControl & FlowControl.AnnotationMask;
			set => _flowControl = (_flowControl & FlowControl.TypeMask) | (value & FlowControl.AnnotationMask);
		}

		/// <inheritdoc />
		public FlowControl FlowControl {
			get => _flowControl;
			set => _flowControl = value;
		}

		/// <summary>
		/// Returns fall through and throws if null
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public BasicBlock FallThrough => FallThroughNoThrow ?? throw new ArgumentNullException(nameof(FallThroughNoThrow));

		/// <summary>
		/// Returns fall through of current basic block
		/// </summary>
		public BasicBlock? FallThroughNoThrow {
			get => _fallThrough;
			set => _fallThrough = UpdateReferences(_fallThrough, value);
		}

		/// <summary>
		/// Returns conditional target and throws if null
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public BasicBlock CondTarget => CondTargetNoThrow ?? throw new ArgumentNullException(nameof(CondTargetNoThrow));

		/// <summary>
		/// Returns the conditional branch of current basic block (jumps into it if condition is true)
		/// </summary>
		public BasicBlock? CondTargetNoThrow {
			get => _condTarget;
			set => _condTarget = UpdateReferences(_condTarget, value);
		}

		/// <summary>
		/// Returns switch targets and throws if null
		/// </summary>
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		public TargetList SwitchTargets => SwitchTargetsNoThrow ?? throw new ArgumentNullException(nameof(SwitchTargetsNoThrow));

		/// <summary>
		/// Returns switch targets of current basic block
		/// </summary>
		public TargetList? SwitchTargetsNoThrow {
			get => _switchTargets;
			set {
				if (value is null) {
					if (_switchTargets is null)
						return;

					_switchTargets.Owner = null;
					_switchTargets = null;
				}
				else {
					if (!(value.Owner is null))
						throw new InvalidOperationException($"{nameof(SwitchTargetsNoThrow)} is already owned by another {nameof(BasicBlock)}.");

					if (!(_switchTargets is null))
						_switchTargets.Owner = null;
					value.Owner = this;
					_switchTargets = value;
				}
			}
		}

		/// <summary>
		/// Returns predecessors of current basic block
		/// </summary>
		public IDictionary<BasicBlock, int> Predecessors => _predecessors;

		/// <summary>
		/// Returns successors of current basic block
		/// </summary>
		public IDictionary<BasicBlock, int> Successors => _successors;

#if DEBUG
		internal uint OriginalOffset { get; }
#endif

		/// <summary>
		/// Constructor
		/// </summary>
		public BasicBlock() {
			_instructions = new List<Instruction>();
			_branchInstruction = new Instruction(OpCodes.Ret);
			_predecessors = new BbRefDict<BasicBlock>();
			_successors = new BbRefDict<BasicBlock>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">Instructions in current basic block excluding branch instruction</param>
		public BasicBlock(IEnumerable<Instruction> instructions) : this(instructions, OpCodes.Ret) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">Instructions in current basic block excluding branch instruction</param>
		/// <param name="branchOpcode">Branch opcode of current basic block</param>
		public BasicBlock(IEnumerable<Instruction> instructions, OpCode branchOpcode) : this(instructions, new Instruction(branchOpcode)) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">Instructions in current basic block excluding branch instruction</param>
		/// <param name="branchInstruction">branch instruction of current basic block (dummy instruction)</param>
		public BasicBlock(IEnumerable<Instruction> instructions, Instruction branchInstruction) {
			_instructions = new List<Instruction>(instructions ?? throw new ArgumentNullException(nameof(instructions)));
			_branchInstruction = branchInstruction ?? throw new ArgumentNullException(nameof(branchInstruction));
			branchInstruction.Operand = null;
			BranchOpcode = branchInstruction.OpCode;
			// set flow control
			_predecessors = new BbRefDict<BasicBlock>();
			_successors = new BbRefDict<BasicBlock>();
#if DEBUG
			OriginalOffset = _instructions.Count == 0 ? ushort.MaxValue : _instructions[0].Offset;
#endif
		}

		internal BasicBlock? UpdateReferences(BasicBlock? oldValue, BasicBlock? newValue) {
			if (oldValue != newValue) {
				UpdateReferencesCore(_successors, oldValue, newValue);
				if (!(oldValue is null))
					UpdateReferencesCore(oldValue._predecessors, this, null);
				if (!(newValue is null))
					UpdateReferencesCore(newValue._predecessors, null, this);
			}
			return newValue;
		}

		private static void UpdateReferencesCore(Dictionary<BasicBlock, int> references, BasicBlock? oldValue, BasicBlock? newValue) {
			if (!(oldValue is null)) {
				if (references.TryGetValue(oldValue, out int refCount)) {
					if (refCount == 1)
						references.Remove(oldValue);
					else
						references[oldValue] = refCount - 1;
				}
				else {
					throw new InvalidOperationException();
				}
			}
			if (!(newValue is null)) {
				if (references.TryGetValue(newValue, out int refCount))
					references[newValue] = refCount + 1;
				else
					references[newValue] = 1;
			}
		}

		#region IBasicBlock
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBasicBlock IBasicBlock.FallThrough => FallThrough;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBasicBlock? IBasicBlock.FallThroughNoThrow { get => FallThroughNoThrow; set => FallThroughNoThrow = (BasicBlock?)value; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBasicBlock IBasicBlock.CondTarget => CondTarget;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBasicBlock? IBasicBlock.CondTargetNoThrow { get => CondTargetNoThrow; set => CondTargetNoThrow = (BasicBlock?)value; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ITargetList IBasicBlock.SwitchTargets => SwitchTargets;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		ITargetList? IBasicBlock.SwitchTargetsNoThrow { get => SwitchTargetsNoThrow; set => SwitchTargetsNoThrow = (TargetList?)value; }
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IDictionary<IBasicBlock, int> IBasicBlock.Predecessors => _predecessors;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IDictionary<IBasicBlock, int> IBasicBlock.Successors => _successors;
		#endregion
	}

	/// <summary>
	/// Scope block
	/// </summary>
	[DebuggerTypeProxy(typeof(DebugView))]
	public class ScopeBlock : Block, IScopeBlock {
		/// <summary />
		protected BlockType _type;
		internal BlockList<Block> _blocks;

		/// <inheritdoc />
		public override BlockType Type => _type;

		/// <summary>
		/// Child blocks
		/// </summary>
		public IList<Block> Blocks => _blocks;

		/// <summary>
		/// First block in current scope block
		/// </summary>
		public Block FirstBlock => _blocks[0];

		/// <summary>
		/// Last block in current scope block
		/// </summary>
		public Block LastBlock => _blocks[^1];

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Block type</param>
		public ScopeBlock(BlockType type) {
			_type = type;
			_blocks = new BlockList<Block>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		/// <param name="type">Block type</param>
		public ScopeBlock(IEnumerable<Block> blocks, BlockType type) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			_type = type;
			_blocks = new BlockList<Block>(blocks);
		}

		private sealed class DebugView {
			private readonly Block[] _blocks;

			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public Block[] Blocks => _blocks;

			public DebugView(ScopeBlock scopeBlock) {
				if (scopeBlock is null)
					throw new ArgumentNullException(nameof(scopeBlock));

				_blocks = scopeBlock._blocks.ToArray();
			}
		}

		#region IScopeBlock
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IList<IBlock> IScopeBlock.Blocks => _blocks;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBlock IScopeBlock.FirstBlock => FirstBlock;
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		IBlock IScopeBlock.LastBlock => LastBlock;
		#endregion
	}

	/// <summary>
	/// Handler block
	/// </summary>
	public sealed class HandlerBlock : ScopeBlock, IHandlerBlock {
		private ITypeDefOrRef? _catchType;

		/// <summary>
		/// The catch type if <see cref="Block.Type"/> is <see cref="BlockType.Catch"/>
		/// </summary>
		public ITypeDefOrRef? CatchType {
			get => _catchType;
			set => _catchType = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="type">Block type</param>
		/// <param name="catchType">Catch type if exists</param>
		public HandlerBlock(BlockType type, ITypeDefOrRef? catchType) : base(type) {
			_catchType = catchType;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		/// <param name="type">Block type</param>
		/// <param name="catchType">Catch type if exists</param>
		public HandlerBlock(IEnumerable<Block> blocks, BlockType type, ITypeDefOrRef? catchType) : base(blocks, type) {
			_catchType = catchType;
		}

		#region IHandlerBlock
		[DebuggerBrowsable(DebuggerBrowsableState.Never)]
		object? IHandlerBlock.CatchType { get => CatchType; set => CatchType = (ITypeDefOrRef?)value; }
		#endregion
	}
}
