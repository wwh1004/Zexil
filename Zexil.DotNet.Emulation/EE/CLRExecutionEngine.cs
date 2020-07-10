using System;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.EE {
	public sealed class CLRExecutionEngine : ExecutionEngine {
		public override int Execute(EvaluationStack stack, Instruction instruction, out Exception exception) {
			return Execute((CLREvaluationStack)stack, instruction, out exception);
		}

		public int Execute(CLREvaluationStack stack, Instruction instruction, out Exception exception) {
			throw new NotImplementedException();
		}
	}
}
