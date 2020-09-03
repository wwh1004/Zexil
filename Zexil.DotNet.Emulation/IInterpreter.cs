using System;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interpreter interface
	/// </summary>
	public interface IInterpreter {
		/// <summary>
		/// Interprets an method from <see cref="InterpreterStub"/>
		/// Type conversation (arguments and return value are the same):
		/// refType  -> no conv
		/// refType* -> conv_i
		/// valType  -> box
		/// valType* -> conv_i
		/// genType  -> box
		/// genType* -> conv_i
		/// </summary>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		/// <param name="typeInstantiation"></param>
		/// <param name="methodInstantiation"></param>
		/// <returns></returns>
		object InterpretFromStub(MethodDesc method, object[] arguments, Type[] typeInstantiation, Type[] methodInstantiation);
	}
}
