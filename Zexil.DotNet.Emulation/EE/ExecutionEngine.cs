using System;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.EE {
	public abstract class ExecutionEngine {
		/// <summary>
		/// Executes an instruction and returns a number that indicates control flow.
		/// </summary>
		/// <param name="stack"></param>
		/// <param name="instruction"></param>
		/// <param name="exception"></param>
		/// <returns>
		/// '0' means fall through,
		/// '1' means conditional target,
		/// '1', '2', '3'... means index of a switch target,
		/// '-1' means an exception was thrown.
		/// </returns>
		public abstract int Execute(EvaluationStack stack, Instruction instruction, out Exception exception);
	}
}
