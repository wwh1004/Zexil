using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis.Emit {
	/// <summary>
	/// Block cleaner
	/// </summary>
	public static class BlockCleaner {
		/// <summary>
		/// Removes all nops
		/// </summary>
		/// <param name="blocks"></param>
		/// <returns></returns>
		public static int RemoveNops(IEnumerable<Block> blocks) {
			if (blocks is null)
				throw new ArgumentNullException(nameof(blocks));

			int count = 0;
			foreach (var block in blocks)
				count += RemoveNops(block);
			return count;
		}

		/// <summary>
		/// Removes all nops
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static int RemoveNops(Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			int count = 0;
			foreach (var basicBlock in block.Enumerate<BasicBlock>()) {
				int c = 0;
				var instructions = basicBlock.Instructions;
				for (int i = 0; i < instructions.Count; i++) {
					if (instructions[i].OpCode.Code == Code.Nop)
						c++;
					else
						instructions[i - c] = instructions[i];
				}
				instructions.RemoveRange(instructions.Count - c, c);
				count += c;
			}
			return count;
		}

		/// <summary>
		/// Removes all unused blocks
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <returns></returns>
		public static int RemoveUnusedBlocks(ScopeBlock methodBlock) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));
			if (methodBlock.Type != BlockType.Method)
				throw new ArgumentException($"{nameof(methodBlock)} is not a method block");

			return BlockCleanerCore.RemoveUnusedBlocks(methodBlock, Erase);

			static void Erase(IBasicBlock basicBlock) {
				((BasicBlock)basicBlock).Erase();
			}
		}
	}
}
