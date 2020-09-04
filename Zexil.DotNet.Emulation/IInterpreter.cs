using System;

namespace Zexil.DotNet.Emulation {
	/// <summary>
	/// Interprets an method from <see cref="InterpreterStub"/>
	/// 
	/// Type conversation (arguments and return value are the same):
	/// refType  -> no conv
	/// refType* -> conv_i
	/// valType  -> box
	/// valType* -> conv_i
	/// genType  -> box
	/// genType* -> conv_i
	///
	/// For byref argument, <see cref="InterpreterStubLinker"/> should generate IL to pin byref object if it is a reference type and for byref return,
	/// <see cref="IInterpreter"/> should take measures to prevent GC moving object in a very short time if it is a reference type otherwise native int will pointer to a invalid memory region
	/// </summary>
	/// <param name="method"></param>
	/// <param name="arguments"></param>
	/// <returns></returns>
	public delegate object InterpretFromStubHandler(MethodDesc method, object[] arguments);

	/// <summary>
	/// Interpreter interface
	/// </summary>
	public interface IInterpreter {
		/// <summary>
		/// InterpretFromStub user handler, if <see cref="InterpretFromStubUser"/> is not <see langword="null"/>, <see cref="InterpreterStub.Dispatch(int, int, object[], Type[], Type[])"/> should call
		/// <see cref="InterpretFromStubUser"/> not <see cref="InterpretFromStub(MethodDesc, object[])"/>
		/// </summary>
		InterpretFromStubHandler InterpretFromStubUser { get; set; }

		/// <summary>
		/// Interprets an method from <see cref="InterpreterStub"/>
		/// 
		/// Type conversation (arguments and return value are the same):
		/// refType  -> no conv
		/// refType* -> conv_i
		/// valType  -> box
		/// valType* -> conv_i
		/// genType  -> box
		/// genType* -> conv_i
		///
		/// For byref argument, <see cref="InterpreterStubLinker"/> should generate IL to pin byref object if it is a reference type and for byref return,
		/// <see cref="IInterpreter"/> should take measures to prevent GC moving object in a very short time if it is a reference type otherwise native int will pointer to a invalid memory region
		/// </summary>
		/// <param name="method"></param>
		/// <param name="arguments"></param>
		/// <returns></returns>
		object InterpretFromStub(MethodDesc method, object[] arguments);
	}
}
