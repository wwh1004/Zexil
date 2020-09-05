using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	/// <summary>
	/// Block formatter
	/// </summary>
	public static class BlockFormatter {
		private static readonly object _syncRoot = new object();

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

			return Shared.BlockFormatter.Format_NoLock(new BlockFormatterImpl(), block);
		}

		private sealed class BlockFormatterImpl : Shared.BlockFormatter {
			protected override void FormatBasicBlockCore(IBasicBlock basicBlock) {
				FormatBasicBlockImpl((BasicBlock)basicBlock);
			}

			private void FormatBasicBlockImpl(BasicBlock basicBlock) {
				string blockId = FormatBlockId(basicBlock);
				AppendLine($"// {blockId} (pres: {FormatPredecessors(basicBlock)}, sucs: {FormatSuccessors(basicBlock)})");
#if DEBUG
				AppendLine($"// " +
					$"ofs:IL_{basicBlock.OriginalOffset:X4}" +
					$"{(basicBlock.IsEmpty ? ", empty" : string.Empty)}" +
					$"{(basicBlock.Predecessors.Count == 0 ? ", noref" : string.Empty)}" +
					$"{((basicBlock.Flags & BlockFlags.Erased) == BlockFlags.Erased ? ", erased" : string.Empty)}");
#endif
				for (int i = 0; i < basicBlock.Instructions.Count; i++)
					AppendLine(basicBlock.Instructions[i].ToString());

				var branchInfo = new StringBuilder();
				branchInfo.Append("// opcode:" + basicBlock.BranchOpcode.ToString());
				if (basicBlock.FlowType == FlowControl.Branch) {
					branchInfo.Append($" | fall-through:{FormatBlockId(basicBlock.FallThrough)}");
				}
				else if (basicBlock.FlowType == FlowControl.CondBranch) {
					branchInfo.Append(" | fall-through:" + FormatBlockId(basicBlock.FallThrough));
					if (basicBlock.BranchOpcode.Code == Code.Switch)
						branchInfo.Append($" | switch-targets:{{{string.Join(", ", ((IList<BasicBlock>)basicBlock.SwitchTargets).Select(t => FormatBlockId(t)))}}}");
					else
						branchInfo.Append($" | cond-target:{FormatBlockId(basicBlock.CondTarget)}");
				}

				AppendLine(branchInfo.ToString());
			}

#if DEBUG
			protected override string FormatBlockId(IBasicBlock basicBlock) {
				return $"BLK_{((BasicBlock)basicBlock).OriginalOffset:X4}";
			}
#endif
		}
	}
}
