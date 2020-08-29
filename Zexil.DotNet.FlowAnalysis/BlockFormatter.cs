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
			formatter.SetBlockIds(block);
#endif
			formatter.FormatCore(block);
			return formatter._buffer.ToString();
		}

		private void SetBlockIds(Block block) {
			foreach (var b in block.Enumerate<Block>()) {
				if (b is BasicBlock basicBlock)
					_blockIds[basicBlock] = _currentBlockId++;
			}
		}

		private void FormatCore(Block block) {
			if (block is BasicBlock basicBlock) {
				if (_newLine)
					AppendLine();

				string blockId = FormatBlockId(basicBlock);
				AppendLine($"// {blockId} (pres: {FormatPredecessors(basicBlock)}, sucs: {FormatSuccessors(basicBlock)})");
#if DEBUG
				AppendLine($"// " +
					$"ofs:IL_{basicBlock.OriginalOffset:X4}" +
					$"{(basicBlock.IsEmpty ? ", empty" : string.Empty)}" +
					$"{(basicBlock.Predecessors.Count == 0 ? ", noref" : string.Empty)}" +
					$"{(basicBlock.IsErased ? ", erased" : string.Empty)}");
#endif
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
					AppendLine(basicBlock.Instructions[i].ToString());

				var branchInfo = new StringBuilder();
				branchInfo.Append("// opcode:" + basicBlock.BranchOpcode.ToString());
				if (basicBlock.BranchOpcode.FlowControl == FlowControl.Branch) {
					branchInfo.Append($" | fall-through:{FormatBlockId(basicBlock.FallThrough)}");
				}
				else if (basicBlock.BranchOpcode.FlowControl == FlowControl.Cond_Branch) {
					branchInfo.Append(" | fall-through:" + FormatBlockId(basicBlock.FallThrough));
					if (basicBlock.BranchOpcode.Code == Code.Switch)
						branchInfo.Append($" | switch-targets:{{{string.Join(", ", basicBlock.SwitchTargets.Select(t => FormatBlockId(t)))}}}");
					else
						branchInfo.Append($" | cond-target:{FormatBlockId(basicBlock.CondTarget)}");
				}

				AppendLine(branchInfo.ToString());
				_newLine = true;
			}
			else if (block is ScopeBlock scopeBlock) {
				// enter
				switch (block.Type) {
				case BlockType.Protected:
					if (_newLine)
						AppendLine();
					AppendLine(".protected");
					AppendLine("{");
					break;
				case BlockType.Try:
					AppendLine("try");
					AppendLine("{");
					break;
				case BlockType.Filter:
					AppendLine("filter");
					AppendLine("{");
					break;
				case BlockType.Catch: {
					AppendLine(block is HandlerBlock handlerBlock ? $"catch {handlerBlock.CatchType}" : "catch");
					AppendLine("{");
					break;
				}
				case BlockType.Finally:
					AppendLine("finally");
					AppendLine("{");
					break;
				case BlockType.Fault:
					AppendLine("fault");
					AppendLine("{");
					break;
				case BlockType.Method:
					AppendLine(".method");
					AppendLine("{");
					break;
				default:
					throw new InvalidOperationException();
				}
				_indent += 2;
				_newLine = false;
				// enter scope

				foreach (var child in scopeBlock.Blocks)
					FormatCore(child);
				// format child blocks

				_indent -= 2;
				AppendLine("}");
				_newLine = true;
				// leave scope
			}
			else {
				throw new InvalidOperationException();
			}
		}

		private string FormatBlockId(BasicBlock basicBlock) {
#if DEBUG
			return $"BLK_{basicBlock.OriginalOffset:X4}";
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
