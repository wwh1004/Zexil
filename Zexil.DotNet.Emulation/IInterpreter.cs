using System;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interprets an method from <see cref="InterpreterStub"/>
	/// 
	/// Type conversation (arguments and return value are the same):
	/// refType  -> no conv
	/// refType* -> conv_i
	/// valType  -> ldarga
	/// valType* -> conv_i
	/// genType  -> (ldarga | 0x0001) (we should dereference in runtime if it is reference type, for fatser, we mark the address)
	/// genType* -> conv_i
	///
	/// Calling conversation
	/// arguments = method arguments + method return buffer (if method has return value)
	/// </summary>
	/// <param name="interpreter"></param>
	/// <param name="method"></param>
	/// <param name="arguments"></param>
	public delegate void InterpretFromStubHandler(IInterpreter interpreter, MethodDesc method, nint[] arguments);

	/// <summary>
	/// Interpreter interface
	/// </summary>
	public interface IInterpreter {
		/// <summary>
		/// InterpretFromStub user handler, if <see cref="InterpretFromStubUser"/> is not <see langword="null"/>, <see cref="InterpreterStub.Dispatch(int, int, nint[], Type[], Type[])"/> should call
		/// <see cref="InterpretFromStubUser"/> not <see cref="InterpretFromStub(MethodDesc, nint[])"/>
		/// </summary>
		InterpretFromStubHandler InterpretFromStubUser { get; set; }

		/// <summary>
		/// Interprets an method from <see cref="InterpreterStub"/>
		/// 
		/// Type conversation (arguments and return value are the same):
		/// refType  -> no conv
		/// refType* -> conv_i
		/// valType  -> ldarga
		/// valType* -> conv_i
		/// genType  -> (ldarga | 0x0001) (we should dereference in runtime if it is reference type, for fatser, we mark the address)
		/// genType* -> conv_i
		///
		/// Calling conversation
		/// arguments = method arguments + method return buffer (if method has return value)
		/// </summary>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		void InterpretFromStub(MethodDesc method, nint[] arguments);
	}
}
