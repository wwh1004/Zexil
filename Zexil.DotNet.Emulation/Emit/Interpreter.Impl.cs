using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.Emit {
	partial class Interpreter {
		private void InterpretImpl(Instruction instruction, InterpreterMethodContext methodContext) {
			// 运算单独一个方法，用switch判断操作符。提高执行速度，不要用委托
		}
	}
}
