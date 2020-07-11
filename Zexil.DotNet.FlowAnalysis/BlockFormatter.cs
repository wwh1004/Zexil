using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Block formatter
	/// </summary>
	public sealed class BlockFormatter {
		private static readonly object _syncRoot = new object();

		private readonly StringBuilder _buffer;
		private readonly Dictionary<BasicBlock, int> _blockIds;
		private int _currentBlockId;
		private int _indent;
		private bool _newLine;

		private BlockFormatter() {
			_buffer = new StringBuilder();
			_blockIds = new Dictionary<BasicBlock, int>();
		}

		/// <summary>
		/// Thread safe version of <see cref="Format_NoLock(Block)"/>
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static string Format(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			lock (_syncRoot)
				return Format_NoLock(block);
		}

		/// <summary>
		/// Formats a block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static string Format_NoLock(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			var formatter = new BlockFormatter();
#if !DEBUG
			BlockVisitor.VisitAll(block, onBlockEnter: formatter.OnBlockEnter_SetBlockId);
#endif
			BlockVisitor.VisitAll(block, formatter.OnBlockEnter, formatter.OnBlockLeave);
			return formatter._buffer.ToString();
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
					branchInfo.Append(" | fall-through:" + FormatBlockId(basicBlock.FallThrough));
				}
				else if (basicBlock.BranchOpcode.FlowControl == FlowControl.Cond_Branch) {
					branchInfo.Append(" | fall-through:" + FormatBlockId(basicBlock.FallThrough));
					if (basicBlock.BranchOpcode.Code == Code.Switch) {
						branchInfo.Append(" | switch-targets:{");
						foreach (var switchTarget in basicBlock.SwitchTargets)
							branchInfo.Append(FormatBlockId(switchTarget) + " ");
						branchInfo[^1] = '}';
					}
					else {
						branchInfo.Append(" | cond-target:" + FormatBlockId(basicBlock.CondTarget));
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

		private bool OnBlockEnter_SetBlockId(Block block) {
			if (block is BasicBlock basicBlock)
				_blockIds[basicBlock] = _currentBlockId++;
			return false;
		}

		private string FormatBlockId(BasicBlock basicBlock) {
#if DEBUG
			return $"BLK_{basicBlock._originalOffset:X4}";
#else
			return _blockIds.TryGetValue(basicBlock, out int blockId) ? $"BLK_{blockId:X4}" : "BLK_????";
#endif
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
	}
}
