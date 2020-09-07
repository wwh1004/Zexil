using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.Emit {
	partial class Interpreter {
		private void InterpretImpl(Instruction instruction, InterpreterMethodContext methodContext) {
			switch (instruction.OpCode.Code) {
			case Code.Add:
			case Code.Add_Ovf:
			case Code.Add_Ovf_Un:
			case Code.And:
			case Code.Div:
			case Code.Div_Un:
			case Code.Mul:
			case Code.Mul_Ovf:
			case Code.Mul_Ovf_Un:
			case Code.Neg:
			case Code.Not:
			case Code.Or:
			case Code.Rem:
			case Code.Rem_Un:
			case Code.Shl:
			case Code.Shr:
			case Code.Shr_Un:
			case Code.Sub:
			case Code.Sub_Ovf:
			case Code.Sub_Ovf_Un:
			case Code.Xor:
				break;

			default:
				break;
			}
		}

		private void InterpretArithmetic(InterpreterMethodContext methodContext) {

		}
	}
}
