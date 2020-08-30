using System;
using System.Linq;
using System.Text;

namespace Zexil.DotNet.FlowAnalysis {
	internal abstract class BlockFormatterCore {
		protected readonly StringBuilder _buffer;
#if !DEBUG
		protected readonly Dictionary<IBasicBlock, int> _blockIds;
		protected int _currentBlockId;
#endif
		protected int _indent;
		protected bool _newLine;

		protected BlockFormatterCore() {
			_buffer = new StringBuilder();
#if !DEBUG
			_blockIds = new Dictionary<IBasicBlock, int>();
#endif
		}

		public static string Format_NoLock(BlockFormatterCore formatter, IBlock block) {
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
					AppendLine(block is IHandlerBlock handlerBlock ? $"catch {handlerBlock.CatchType}" : "catch");
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

		protected abstract void FormatBasicBlockCore(IBasicBlock basicBlock);

		protected virtual string FormatBlockId(IBasicBlock basicBlock) {
#if DEBUG
			throw new NotImplementedException();
#else
			return _blockIds.TryGetValue(basicBlock, out int blockId) ? $"BLK_{blockId:X4}" : "BLK_????";
#endif
		}

		protected virtual string FormatPredecessors(IBasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Predecessors.Select(t => FormatBlockId(t.Key)));
		}

		protected virtual string FormatSuccessors(IBasicBlock basicBlock) {
			return string.Join(", ", basicBlock.Successors.Select(t => FormatBlockId(t.Key)));
		}

		protected void AppendLine() {
			_buffer.AppendLine();
		}

		protected virtual void AppendLine(string value) {
			_buffer.Append(' ', _indent);
			_buffer.AppendLine(value);
		}
	}
}
