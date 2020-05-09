using System;
using System.Linq;
using System.Text;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block printer
	/// </summary>
	public sealed class BlockPrinter {
		private static readonly object _syncRoot = new object();

		private readonly StringBuilder _buffer;
		private int _indent;
		private bool _newLine;

		private BlockPrinter() {
			_buffer = new StringBuilder();
		}

		/// <summary>
		/// Thread safe version of <see cref="ToString(Block)"/>
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static string ToString_ThreadSafe(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			lock (_syncRoot)
				return ToString(block);
		}

		/// <summary>
		/// Converts a block to a string
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static string ToString(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			var printer = new BlockPrinter();
			printer.SetContexts(block);
			BlockVisitor.VisitAll(block, printer.OnBlockEnter, printer.OnBlockLeave);
			printer.RemoveContexts(block);
			return printer._buffer.ToString();
		}

		private void SetContexts(Block block) {
			int id = 0;
			BlockVisitor.VisitAll(block, onBlockEnter: b => {
				if (b is BasicBlock basicBlock)
					basicBlock.Contexts.Set(this, new BlockContext(id++));
				return false;
			});
		}

		private void RemoveContexts(Block block) {
			BlockVisitor.VisitAll(block, onBlockEnter: b => {
				if (b is BasicBlock basicBlock)
					basicBlock.Contexts.Remove<BlockContext>(this);
				return false;
			});
		}

		private bool OnBlockEnter(Block block) {
			if (block is BasicBlock basicBlock) {
				if (_newLine)
					AppendLine();

				string blockId = FormatBlockId(basicBlock);
				if (basicBlock.IsEmpty)
					blockId += "(empty)";
				AppendLine($"// {blockId} (pres: {FormatPredecessors(basicBlock)}, sucs: {FormatSuccessors(basicBlock)})");
#if DEBUG
				AppendLine("// ofs:IL_" + basicBlock._originalOffset.ToString("X4"));
#endif
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
					AppendLine(basicBlock.Instructions[i].ToString());

				var branchInfo = new StringBuilder();
				branchInfo.Append("// opcode:" + basicBlock.BranchOpcode.ToString());
				if (basicBlock.BranchOpcode.FlowControl == FlowControl.Branch) {
					if (basicBlock.FallThroughTarget is null)
						throw new InvalidOperationException();
					branchInfo.Append(" | fallthrough:" + FormatBlockId(basicBlock.FallThroughTarget));
				}
				else if (basicBlock.BranchOpcode.FlowControl == FlowControl.Cond_Branch) {
					if (basicBlock.FallThroughTarget is null)
						throw new InvalidOperationException();
					branchInfo.Append(" | fallthrough:" + FormatBlockId(basicBlock.FallThroughTarget));
					if (basicBlock.BranchOpcode.Code == Code.Switch) {
						if (basicBlock.SwitchTargets is null)
							throw new InvalidOperationException();
						branchInfo.Append(" | switchtarget:{");
						foreach (var target in basicBlock.SwitchTargets)
							branchInfo.Append(FormatBlockId(target) + " ");
						branchInfo[^1] = '}';
					}
					else {
						if (basicBlock.ConditionalTarget is null)
							throw new InvalidOperationException();
						branchInfo.Append(" | condtarget:" + FormatBlockId(basicBlock.ConditionalTarget));
					}
				}

				AppendLine(branchInfo.ToString());
				_newLine = true;
			}
			else if (block is TryBlock tryBlock) {
				if (_newLine)
					AppendLine();
				AppendLine(".try");
				AppendLine("{");
				_indent += 2;
				_newLine = false;
			}
			else if (block is FilterBlock filterBlock) {
				AppendLine("filter");
				AppendLine("{");
				_indent += 2;
				_newLine = false;
			}
			else if (block is HandlerBlock handlerBlock) {
				switch (handlerBlock.Type) {
					case BlockType.Catch: {
						if (handlerBlock.CatchType is null)
							AppendLine("catch");
						else
							AppendLine($"catch {handlerBlock.CatchType}");
						break;
					}
					case BlockType.Finally: {
						AppendLine("finally");
						break;
					}
					case BlockType.Fault: {
						AppendLine("fault");
						break;
					}
					default: {
						throw new InvalidOperationException();
					}
				}
				AppendLine("{");
				_indent += 2;
				_newLine = false;
			}
			else if (block is MethodBlock methodBlock) {
				AppendLine(".method");
				AppendLine("{");
				_indent += 2;
				_newLine = false;
			}
			else {
				throw new InvalidOperationException();
			}
			return false;
		}

		private bool OnBlockLeave(Block block) {
			if (block is ScopeBlock) {
				_indent -= 2;
				AppendLine("}");
				_newLine = true;
			}
			return false;
		}

		private string FormatBlockId(BasicBlock basicBlock) {
			if (basicBlock.Contexts.TryGet<BlockContext>(this, out var context))
				return $"BLK_{context.Id:X4}";
			else
				return "BLK_????";
		}

		private string FormatPredecessors(BasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Predecessors.Select(t => FormatBlockId(t.Key)));
		}

		private string FormatSuccessors(BasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Successors.Select(t => FormatBlockId(t.Key)));
		}

		private void AppendLine() {
			_buffer.AppendLine();
		}

		private void AppendLine(string value) {
			_buffer.Append(' ', _indent);
			_buffer.AppendLine(value);
		}

		private sealed class BlockContext : IBlockContext {
			private readonly int _id;

			public int Id => _id;

			public BlockContext(int id) {
				_id = id;
			}
		}
	}
}
