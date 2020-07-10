using System;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.EE {
	public sealed class ZExecutionEngine : ExecutionEngine {
		public override int Execute(EvaluationStack stack, Instruction instruction, out Exception exception) {
			return Execute((ZEvaluationStack)stack, instruction, out exception);
		}

		public int Execute(ZEvaluationStack stack, Instruction instruction, out Exception exception) {
			throw new NotImplementedException();
		}
	}
}
