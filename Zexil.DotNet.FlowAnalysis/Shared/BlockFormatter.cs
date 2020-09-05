using System;
using System.Linq;
using System.Text;

namespace Zexil.DotNet.FlowAnalysis.Shared {
	/// <summary>
	/// Block formatter common code
	/// </summary>
	public abstract class BlockFormatter {
		/// <summary />
		protected readonly StringBuilder _buffer;
#if !DEBUG
		/// <summary />
		protected readonly System.Collections.Generic.Dictionary<IBasicBlock, int> _blockIds;
		/// <summary />
		protected int _currentBlockId;
#endif
		/// <summary />
		protected int _indent;
		/// <summary />
		protected bool _newLine;

		/// <summary />
		protected BlockFormatter() {
			_buffer = new StringBuilder();
#if !DEBUG
			_blockIds = new System.Collections.Generic.Dictionary<IBasicBlock, int>();
#endif
		}

		/// <summary>
		/// Formats a block
		/// </summary>
		/// <param name="formatter"></param>
		/// <param name="block"></param>
		/// <returns></returns>
		public static string Format_NoLock(BlockFormatter formatter, IBlock block) {
#if !DEBUG
			formatter.SetBlockIds(block);
#endif
			formatter.FormatCore(block);
			return formatter._buffer.ToString();
		}

#if !DEBUG
		private void SetBlockIds(IBlock block) {
			foreach (var b in block.Enumerate<IBlock>()) {
				if (b is IBasicBlock basicBlock)
					_blockIds[basicBlock] = _currentBlockId++;
			}
		}
#endif

		/// <summary />
		protected virtual void FormatCore(IBlock block) {
			if (block is IBasicBlock basicBlock) {
				if (_newLine)
					AppendLine();

				FormatBasicBlockCore(basicBlock);
				_newLine = true;
			}
			else if (block is IScopeBlock scopeBlock) {
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
					object? catchType = ((IHandlerBlock)block).CatchType;
					AppendLine(!(catchType is null) ? $"catch {catchType}" : "catch null");
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

		/// <summary />
		protected abstract void FormatBasicBlockCore(IBasicBlock basicBlock);

		/// <summary />
		protected virtual string FormatBlockId(IBasicBlock basicBlock) {
#if DEBUG
			throw new NotImplementedException();
#else
			return _blockIds.TryGetValue(basicBlock, out int blockId) ? $"BLK_{blockId:X4}" : "BLK_????";
#endif
		}

		/// <summary />
		protected virtual string FormatPredecessors(IBasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Predecessors.Keys.Select(t => FormatBlockId(t)));
		}

		/// <summary />
		protected virtual string FormatSuccessors(IBasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Successors.Keys.Select(t => FormatBlockId(t)));
		}

		/// <summary />
		protected void AppendLine() {
			_buffer.AppendLine();
		}

		/// <summary />
		protected virtual void AppendLine(string value) {
			_buffer.Append(' ', _indent);
			_buffer.AppendLine(value);
		}
	}
}
