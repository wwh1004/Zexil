using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
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
		Method,

		/// <summary>
		/// Block defined by user
		/// </summary>
		User
	}

	/// <summary>
	/// Represents a block context
	/// </summary>
	public interface IBlockContext {
	}

	/// <summary>
	/// A collection of block contexts
	/// </summary>
	public sealed class BlockContexts {
		private readonly LinkedList<IBlockContext> _contexts;

		internal BlockContexts() {
			_contexts = new LinkedList<IBlockContext>();
		}

		/// <summary>
		/// Add a new context
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context">A block context</param>
		/// <returns></returns>
		public T Add<T>(T context) where T : class, IBlockContext {
			if (context is null)
				throw new ArgumentNullException(nameof(context));

			_contexts.AddLast(context);
			return context;
		}

		/// <summary>
		/// Remove the latest context
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Remove<T>() where T : class, IBlockContext {
			var t = Peek<T>();
			_contexts.RemoveLast();
			return t;
		}

		/// <summary>
		/// Peek the latest context
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public T Peek<T>() where T : class, IBlockContext {
			if (_contexts.Count == 0)
				throw new InvalidOperationException($"{nameof(_contexts)} is empty");

			if (!(_contexts.Last.Value is T t))
				throw new InvalidCastException($"Real type of current context is {_contexts.Last.Value.GetType()} and it can't be convert into {typeof(T)}");
			return t;
		}
	}

	/// <summary>
	/// Represents a context
	/// </summary>
	public abstract class Block {
		private Block? _scope;
		private readonly BlockContexts _contexts;

		/// <summary>
		/// Default constructor
		/// </summary>
		protected internal Block() {
			_contexts = new BlockContexts();
		}

		/// <summary>
		/// Block type
		/// </summary>
		public abstract BlockType Type { get; }

		/// <summary>
		/// Scope block of current block
		/// </summary>
		public Block? Scope {
			get => _scope;
			set => _scope = value;
		}

		/// <summary>
		/// Block contexts
		/// </summary>
		public BlockContexts Contexts => _contexts;
	}

	/// <summary>
	/// Basic block
	/// </summary>
	[DebuggerDisplay("{ToDebugString()}")]
	public sealed class BasicBlock : Block {
		private readonly List<Instruction> _instructions;
		private OpCode _branchOpcode;
		private BasicBlock? _fallThroughTarget;
		private BasicBlock? _conditionalTarget;
		private IList<BasicBlock>? _switchTargets;
#if DEBUG
		internal readonly uint _originalOffset;
#endif

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
		/// Returns branch opcode of current basic block
		/// </summary>
		public OpCode BranchOpcode {
			get => _branchOpcode;
			set => _branchOpcode = value;
		}

		/// <summary>
		/// Returns fallthrough basic block of current basic block
		/// </summary>
		public BasicBlock? FallThroughTarget {
			get => _fallThroughTarget;
			set => _fallThroughTarget = value;
		}

		/// <summary>
		/// Returns the conditional basic block of current basic block (jumps into it if condition is true)
		/// </summary>
		public BasicBlock? ConditionalTarget {
			get => _conditionalTarget;
			set => _conditionalTarget = value;
		}

		/// <summary>
		/// Returns switch target basic block of current basic block
		/// </summary>
		public IList<BasicBlock>? SwitchTargets {
			get => _switchTargets;
			set => _switchTargets = value;
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">Instructions in current basic block excluding branch instruction</param>
		public BasicBlock(IEnumerable<Instruction> instructions) : this(instructions, OpCodes.Nop) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="instructions">Instructions in current basic block excluding branch instruction</param>
		/// <param name="branchOpcode">Branch opcode of current basic block</param>
		public BasicBlock(IEnumerable<Instruction> instructions, OpCode branchOpcode) {
			if (instructions is null)
				throw new ArgumentNullException(nameof(instructions));
			if (branchOpcode is null)
				throw new ArgumentNullException(nameof(branchOpcode));

			_instructions = new List<Instruction>(instructions);
			_branchOpcode = branchOpcode;
#if DEBUG
			_originalOffset = _instructions.Count == 0 ? ushort.MaxValue : _instructions[0].Offset;
#endif
		}

		private string ToDebugString() {
			var sb = new StringBuilder();
			if (IsEmpty) {
				sb.Append("empty");
			}
			else {
				sb.Append("ofs:IL_");
				sb.Append(_instructions[0].Offset.ToString("X4"));
			}
			sb.Append(' ');
			sb.Append(_branchOpcode.ToString());
#if DEBUG
			sb.Append(" | __ofs:IL_");
			sb.Append(_originalOffset.ToString("X4"));
#endif
			return sb.ToString();
		}
	}

	/// <summary>
	/// Scope block
	/// </summary>
	[DebuggerDisplay("L:{Blocks.Count} T:{Type.ToString()}")]
	[DebuggerTypeProxy(typeof(DebugView))]
	public abstract class ScopeBlock : Block {
		/// <summary />
		protected List<Block> _blocks;
		/// <summary />
		protected BlockType _type;

		/// <summary>
		/// Child blocks
		/// </summary>
		public IList<Block> Blocks => _blocks;

		/// <summary>
		/// First block in current scope block
		/// </summary>
		public Block FirstBlock {
			get => _blocks[0];
			set => _blocks[0] = value;
		}

		/// <summary>
		/// Last block in current scope block
		/// </summary>
		public Block LastBlock {
			get => _blocks[^1];
			set => _blocks[^1] = value;
		}

		/// <summary>
		/// Block type
		/// </summary>
		public override BlockType Type => _type;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		/// <param name="type">Block type</param>
		protected internal ScopeBlock(IEnumerable<Block> blocks, BlockType type) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			_blocks = new List<Block>(blocks);
			_type = type;
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
	}

	/// <summary>
	/// Try block
	/// </summary>
	public sealed class TryBlock : ScopeBlock {
		private readonly List<HandlerBlock> _handlers;

		/// <summary>
		/// Handler blocks
		/// </summary>
		public IList<HandlerBlock> Handlers => _handlers;

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		public TryBlock(IEnumerable<Block> blocks) : base(blocks, BlockType.Try) {
			_handlers = new List<HandlerBlock>();
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		/// <param name="handlers">Handler blocks</param>
		public TryBlock(IEnumerable<Block> blocks, IEnumerable<HandlerBlock> handlers) : base(blocks, BlockType.Try) {
			_handlers = new List<HandlerBlock>(handlers);
		}
	}

	/// <summary>
	/// Filter block
	/// </summary>
	public sealed class FilterBlock : ScopeBlock {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		public FilterBlock(IEnumerable<Block> blocks) : base(blocks, BlockType.Filter) {
		}
	}

	/// <summary>
	/// Handler block
	/// </summary>
	public sealed class HandlerBlock : ScopeBlock {
		private FilterBlock? _filter;
		private ITypeDefOrRef? _catchType;

		/// <summary>
		/// Filter block
		/// </summary>
		public FilterBlock? Filter {
			get => _filter;
			set => _filter = value;
		}

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
		/// <param name="blocks">Child blocks</param>
		/// <param name="type">Block type</param>
		/// <param name="catchType">Catch type if exists</param>
		public HandlerBlock(IEnumerable<Block> blocks, BlockType type, ITypeDefOrRef? catchType) : this(blocks, type, null, catchType) {
		}

		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		/// <param name="type">Block type</param>
		/// <param name="filter">Filter block if exists</param>
		/// <param name="catchType">Catch type if exists</param>
		public HandlerBlock(IEnumerable<Block> blocks, BlockType type, FilterBlock? filter, ITypeDefOrRef? catchType) : base(blocks, type) {
			if (!IsValidHandlerBlockType(type))
				throw new ArgumentOutOfRangeException(nameof(type));

			_filter = filter;
			_catchType = catchType;
		}

		private static bool IsValidHandlerBlockType(BlockType type) {
			return type switch
			{
				BlockType.Catch => true,
				BlockType.Finally => true,
				BlockType.Fault => true,
				_ => false,
			};
		}
	}

	/// <summary>
	/// Method block
	/// </summary>
	public sealed class MethodBlock : ScopeBlock {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		public MethodBlock(IEnumerable<Block> blocks) : base(blocks, BlockType.Method) {
		}
	}

	/// <summary>
	/// User block
	/// </summary>
	public abstract class UserBlock : ScopeBlock {
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="blocks">Child blocks</param>
		protected UserBlock(IEnumerable<Block> blocks) : base(blocks, BlockType.User) {
		}
	}
}
