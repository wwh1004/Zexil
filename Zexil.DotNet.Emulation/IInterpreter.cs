using System;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter interface
	/// </summary>
	public interface IInterpreter {
		/// <summary>
		/// Interprets an method from <see cref="InterpreterStub"/>
		/// </summary>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		object InterpretFromStub(MethodDesc method, object[] arguments, Type[] typeInstantiation, Type[] methodInstantiation);
	}
}
