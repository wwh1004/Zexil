using System;
using System.Collections.Generic;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Block and instructions converter
	/// </summary>
	public static class BlockConverter {
		/// <summary>
		/// Converts instructions into method block
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="exceptionHandlers"></param>
		/// <returns></returns>
		public static MethodBlock ToMethodBlock(IList<Instruction> instructions, IList<ExceptionHandler> exceptionHandlers) {
			if (instructions is null)
				throw new ArgumentNullException(nameof(instructions));
			if (exceptionHandlers is null)
				throw new ArgumentNullException(nameof(exceptionHandlers));

			return CodeParser.Parse(instructions, exceptionHandlers);
		}

		/// <summary>
		/// Converts method block into instructions
		/// </summary>
		/// <param name="methodBlock"></param>
		/// <param name="instructions"></param>
		/// <param name="exceptionHandlers"></param>
		/// <param name="locals"></param>
		public static void ToInstructions(MethodBlock methodBlock, out IList<Instruction> instructions, out IList<ExceptionHandler> exceptionHandlers, out IList<Local> locals) {
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));

			CodeGenerator.Generate(methodBlock, out instructions, out exceptionHandlers, out locals);
		}
	}
}
