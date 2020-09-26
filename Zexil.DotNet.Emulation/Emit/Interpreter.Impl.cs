using System;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.Emulation.Emit {
	unsafe partial class Interpreter {
		private void InterpretImpl(Instruction instruction, InterpreterMethodContext methodContext) {
			switch (instruction.OpCode.Code) {
			case Code.Add: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 + v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I4 + v2.I8);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 + v2.I);
					else if (v2.IsByRef)
						methodContext.PushByRef(v1.I4 + v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 + v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 + v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 + v2.I);
					else if (v2.IsByRef)
						methodContext.PushByRef((nint)(v1.I8 + v2.I));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(v1.I + v2.I);
					else if (v2.IsI4)
						methodContext.PushI(v1.I + v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I + v2.I8);
					else if (v2.IsByRef)
						methodContext.PushByRef(v1.I + v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsI4)
						methodContext.PushByRef(v1.I + v2.I4);
					else if (v2.IsI8)
						methodContext.PushByRef((nint)(v1.I + v2.I8));
					else if (v2.IsI)
						methodContext.PushByRef(v1.I + v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (byref and ?)");
					break;
				case ElementType.R4:
					if (v2.IsR4)
						methodContext.PushR4(v1.R4 + v2.R4);
					else if (v2.IsR8)
						methodContext.PushR8(v1.R8 + v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (float and ?)");
					break;
				case ElementType.R8:
					if (v2.IsR8)
						methodContext.PushR8(v1.R8 + v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (double and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Add_Ovf: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(checked(v1.I4 + v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I4 + v2.I8));
					else if (v2.IsI)
						methodContext.PushI(checked(v1.I + v2.I));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(checked(v1.I8 + v2.I8));
					else if (v2.IsI4)
						methodContext.PushI8(checked(v1.I8 + v2.I4));
					else if (v2.IsI)
						methodContext.PushI8(checked(v1.I8 + v2.I));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for long and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(checked(v1.I + v2.I));
					else if (v2.IsI4)
						methodContext.PushI(checked(v1.I + v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I + v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for native int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					throw new InvalidProgramException("Signed binary arithmetic overflow operation not permitted on managed pointer values.");
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Add_Ovf_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)checked(v1.U4 + v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U4 + v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U4 + v2.U));
					else if (v2.IsByRef)
						methodContext.PushByRef((nint)checked(v1.U4 + v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U8 + v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)checked(v1.U8 + v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)checked(v1.U8 + v2.U));
					else if (v2.IsByRef)
						methodContext.PushByRef((nint)checked(v1.U8 + v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U + v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)checked(v1.U + v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U + v2.U8));
					else if (v2.IsByRef)
						methodContext.PushByRef((nint)checked(v1.U + v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsI4)
						methodContext.PushByRef((nint)checked(v1.U + v2.U4));
					else if (v2.IsI8)
						methodContext.PushByRef((nint)checked(v1.U + v2.U8));
					else if (v2.IsI)
						methodContext.PushByRef((nint)checked(v1.U + v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (byref and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Sub: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 - v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I4 - v2.I8);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 - v2.I);
					else if (v2.IsByRef)
						throw new InvalidProgramException("Operation not permitted on int and managed pointer.");
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 - v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 - v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 - v2.I);
					else if (v2.IsByRef)
						throw new InvalidProgramException("Operation not permitted on long and managed pointer.");
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(v1.I - v2.I);
					else if (v2.IsI4)
						methodContext.PushI(v1.I - v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I - v2.I8);
					else if (v2.IsByRef)
						throw new InvalidProgramException("Operation not permitted on native int and managed pointer.");
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsByRef)
						methodContext.PushI(v1.I - v2.I);
					else if (v2.IsI4)
						methodContext.PushByRef(v1.I - v2.I4);
					else if (v2.IsI8)
						methodContext.PushByRef((nint)(v1.I - v2.I8));
					else if (v2.IsI)
						methodContext.PushByRef(v1.I - v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (byref and ?)");
					break;
				case ElementType.R4:
					if (v2.IsR4)
						methodContext.PushR4(v1.R4 - v2.R4);
					else if (v2.IsR8)
						methodContext.PushR8(v1.R8 - v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (float and ?)");
					break;
				case ElementType.R8:
					if (v2.IsR8)
						methodContext.PushR8(v1.R8 - v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (double and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Sub_Ovf: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(checked(v1.I4 - v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I4 - v2.I8));
					else if (v2.IsI)
						methodContext.PushI(checked(v1.I4 - v2.I));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(checked(v1.I8 - v2.I8));
					else if (v2.IsI4)
						methodContext.PushI8(checked(v1.I8 - v2.I4));
					else if (v2.IsI)
						methodContext.PushI8(checked(v1.I8 - v2.I));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for long and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(checked(v1.I - v2.I));
					else if (v2.IsI4)
						methodContext.PushI(checked(v1.I - v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I - v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for native int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					throw new InvalidProgramException("Signed binary arithmetic overflow operation not permitted on managed pointer values.");
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Sub_Ovf_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)checked(v1.U4 - v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U4 - v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U4 - v2.U));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U8 - v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)checked(v1.U8 - v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)checked(v1.U8 - v2.U));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for long and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U - v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)checked(v1.U - v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U - v2.U8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for native int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsByRef)
						methodContext.PushI((nint)checked(v1.U - v2.U));
					else if (v2.IsI4)
						methodContext.PushByRef((nint)checked(v1.U - v2.U4));
					else if (v2.IsI8)
						methodContext.PushByRef((nint)checked(v1.U - v2.U8));
					else if (v2.IsI)
						methodContext.PushByRef((nint)checked(v1.U - v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (byref and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Mul: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 * v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I4 * v2.I8);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 * v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 * v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 * v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 * v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(v1.I * v2.I);
					else if (v2.IsI4)
						methodContext.PushI(v1.I * v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I * v2.I8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.R4:
					if (v2.IsR4)
						methodContext.PushR4(v1.R4 * v2.R4);
					else if (v2.IsR8)
						methodContext.PushR8(v1.R8 * v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (float and ?)");
					break;
				case ElementType.R8:
					if (v2.IsR8)
						methodContext.PushR8(v1.R8 * v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (double and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Mul_Ovf: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(checked(v1.I4 * v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I4 * v2.I8));
					else if (v2.IsI)
						methodContext.PushI(checked(v1.I4 * v2.I));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(checked(v1.I8 * v2.I8));
					else if (v2.IsI4)
						methodContext.PushI8(checked(v1.I8 * v2.I4));
					else if (v2.IsI)
						methodContext.PushI8(checked(v1.I8 * v2.I));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(checked(v1.I * v2.I));
					else if (v2.IsI4)
						methodContext.PushI(checked(v1.I * v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(v1.I * v2.I8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non*stack*normal type on stack.");
				}
				break;
			}
			case Code.Mul_Ovf_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)checked(v1.U4 * v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U4 * v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U4 * v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U8 * v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)checked(v1.U8 * v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)checked(v1.U8 * v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)checked(v1.U * v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)checked(v1.U * v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U * v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non*stack*normal type on stack.");
				}
				break;
			}
			case Code.Div: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 / v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I4 / v2.I8);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 / v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 / v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 / v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 / v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(v1.I / v2.I);
					else if (v2.IsI4)
						methodContext.PushI(v1.I / v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I / v2.I8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.R4:
					if (v2.IsR4)
						methodContext.PushR4(v1.R4 / v2.R4);
					else if (v2.IsR8)
						methodContext.PushR8(v1.R8 / v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (float and ?)");
					break;
				case ElementType.R8:
					if (v2.IsR8)
						methodContext.PushR8(v1.R8 / v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (double and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Div_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 / v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U4 / v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 / v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)(v1.U8 / v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 / v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 / v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)(v1.U / v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)(v1.U / v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U / v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non/stack/normal type on stack.");
				}
				break;
			}
			case Code.Rem: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 % v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I4 % v2.I8);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 % v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 % v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 % v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 % v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI(v1.I % v2.I);
					else if (v2.IsI4)
						methodContext.PushI(v1.I % v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8(v1.I % v2.I8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.R4:
					if (v2.IsR4)
						methodContext.PushR4(v1.R4 % v2.R4);
					else if (v2.IsR8)
						methodContext.PushR8(v1.R8 % v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (float and ?)");
					break;
				case ElementType.R8:
					if (v2.IsR8)
						methodContext.PushR8(v1.R8 % v2.R8);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (double and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Rem_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 % v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U4 % v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 % v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)(v1.U8 % v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 % v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 % v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)(v1.U % v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)(v1.U % v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U % v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non%stack%normal type on stack.");
				}
				break;
			}
			case Code.And: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 & v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U4 & v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 & v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)(v1.U8 & v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 & v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 & v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)(v1.U & v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)(v1.U & v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U & v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Illegal operation for non-integral data type.");
				}
				break;
			}
			case Code.Or: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 | v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U4 | v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 | v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)(v1.U8 | v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 | v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 | v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)(v1.U | v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)(v1.U | v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U | v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Illegal operation for non-integral data type.");
				}
				break;
			}
			case Code.Xor: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 ^ v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U4 ^ v2.U8));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 ^ v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8((long)(v1.U8 ^ v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 ^ v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 ^ v2.U));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((nint)(v1.U ^ v2.U));
					else if (v2.IsI4)
						methodContext.PushI((nint)(v1.U ^ v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)(v1.U ^ v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				default:
					throw new InvalidProgramException("Illegal operation for non-integral data type.");
				}
				break;
			}
			case Code.Shl: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 << v2.I4);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 << (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I8:
					if (v2.IsI4)
						methodContext.PushI8(v1.I8 << v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 << (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I:
					if (v2.IsI4)
						methodContext.PushI(v1.I << v2.I4);
					else if (v2.IsI)
						methodContext.PushI(v1.I << (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				default:
					throw new InvalidProgramException("Illegal value type for shift operation.");
				}
				break;
			}
			case Code.Shr: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4(v1.I4 >> v2.I4);
					else if (v2.IsI)
						methodContext.PushI(v1.I4 >> (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I8:
					if (v2.IsI4)
						methodContext.PushI8(v1.I8 >> v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 >> (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I:
					if (v2.IsI4)
						methodContext.PushI(v1.I >> v2.I4);
					else if (v2.IsI)
						methodContext.PushI(v1.I >> (int)v2.I);
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				default:
					throw new InvalidProgramException("Illegal value type for shift operation.");
				}
				break;
			}
			case Code.Shr_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.I4:
					if (v2.IsI4)
						methodContext.PushI4((int)(v1.U4 >> v2.I4));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U4 >> (int)v2.I));
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I8:
					if (v2.IsI4)
						methodContext.PushI8((long)(v1.U8 >> v2.I4));
					else if (v2.IsI)
						methodContext.PushI8((long)(v1.U8 >> (int)v2.I));
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				case ElementType.I:
					if (v2.IsI4)
						methodContext.PushI((nint)(v1.U >> v2.I4));
					else if (v2.IsI)
						methodContext.PushI((nint)(v1.U >> (int)v2.I));
					else
						throw new InvalidProgramException("Operand type mismatch for shift operator.");
					break;
				default:
					throw new InvalidProgramException("Illegal value type for shift operation.");
				}
				break;
			}
			case Code.Neg: {
				ref var v = ref methodContext.Peek();
				switch (v.ElementType) {
				case ElementType.I4:
					v.I4 = -v.I4;
					break;
				case ElementType.I8:
					v.I8 = -v.I8;
					break;
				case ElementType.I:
					v.I = -v.I;
					break;
				case ElementType.R4:
					v.R4 = -v.R4;
					break;
				case ElementType.R8:
					v.R8 = -v.R8;
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for Neg operation.");
				}
				break;
			}
			case Code.Not: {
				ref var v = ref methodContext.Peek();
				switch (v.ElementType) {
				case ElementType.I4:
					v.I4 = ~v.I4;
					break;
				case ElementType.I8:
					v.I8 = ~v.I8;
					break;
				case ElementType.I:
					v.I = ~v.I;
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for Not operation.");
				}
				break;
			}

			case Code.Ceq:
			case Code.Cgt:
			case Code.Cgt_Un:
			case Code.Ckfinite:
			case Code.Clt:
			case Code.Clt_Un:
				break;

			case Code.Box:
			case Code.Unbox:
			case Code.Unbox_Any:
			case Code.Castclass:
			case Code.Isinst:
			case Code.Constrained:
				break;

			case Code.Conv_I:
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4:
			case Code.Conv_I8:
			case Code.Conv_Ovf_I:
			case Code.Conv_Ovf_I_Un:
			case Code.Conv_Ovf_I1:
			case Code.Conv_Ovf_I1_Un:
			case Code.Conv_Ovf_I2:
			case Code.Conv_Ovf_I2_Un:
			case Code.Conv_Ovf_I4:
			case Code.Conv_Ovf_I4_Un:
			case Code.Conv_Ovf_I8:
			case Code.Conv_Ovf_I8_Un:
			case Code.Conv_Ovf_U:
			case Code.Conv_Ovf_U_Un:
			case Code.Conv_Ovf_U1:
			case Code.Conv_Ovf_U1_Un:
			case Code.Conv_Ovf_U2:
			case Code.Conv_Ovf_U2_Un:
			case Code.Conv_Ovf_U4:
			case Code.Conv_Ovf_U4_Un:
			case Code.Conv_Ovf_U8:
			case Code.Conv_Ovf_U8_Un:
			case Code.Conv_R_Un:
			case Code.Conv_R4:
			case Code.Conv_R8:
			case Code.Conv_U:
			case Code.Conv_U1:
			case Code.Conv_U2:
			case Code.Conv_U4:
			case Code.Conv_U8:
				break;

			case Code.Dup:
			case Code.Pop:
			case Code.Ldarg:
			case Code.Ldarga:
			case Code.Ldc_I4:
			case Code.Ldc_I8:
			case Code.Ldc_R4:
			case Code.Ldc_R8:
			case Code.Ldelem:
			case Code.Ldelem_I:
			case Code.Ldelem_I1:
			case Code.Ldelem_I2:
			case Code.Ldelem_I4:
			case Code.Ldelem_I8:
			case Code.Ldelem_R4:
			case Code.Ldelem_R8:
			case Code.Ldelem_Ref:
			case Code.Ldelem_U1:
			case Code.Ldelem_U2:
			case Code.Ldelem_U4:
			case Code.Ldelema:
			case Code.Ldfld:
			case Code.Ldflda:
			case Code.Ldftn:
			case Code.Ldind_I:
			case Code.Ldind_I1:
			case Code.Ldind_I2:
			case Code.Ldind_I4:
			case Code.Ldind_I8:
			case Code.Ldind_R4:
			case Code.Ldind_R8:
			case Code.Ldind_Ref:
			case Code.Ldind_U1:
			case Code.Ldind_U2:
			case Code.Ldind_U4:
			case Code.Ldlen:
			case Code.Ldloc:
			case Code.Ldloca:
			case Code.Ldnull:
			case Code.Ldobj:
			case Code.Ldsfld:
			case Code.Ldsflda:
			case Code.Ldstr:
			case Code.Ldtoken:
			case Code.Ldvirtftn:
			case Code.Newarr:
			case Code.Newobj:
			case Code.Starg:
			case Code.Stelem:
			case Code.Stelem_I:
			case Code.Stelem_I1:
			case Code.Stelem_I2:
			case Code.Stelem_I4:
			case Code.Stelem_I8:
			case Code.Stelem_R4:
			case Code.Stelem_R8:
			case Code.Stelem_Ref:
			case Code.Stfld:
			case Code.Stind_I:
			case Code.Stind_I1:
			case Code.Stind_I2:
			case Code.Stind_I4:
			case Code.Stind_I8:
			case Code.Stind_R4:
			case Code.Stind_R8:
			case Code.Stind_Ref:
			case Code.Stloc:
			case Code.Stobj:
			case Code.Stsfld:
				break;

			case Code.Beq:
			case Code.Bge:
			case Code.Bge_Un:
			case Code.Bgt:
			case Code.Bgt_Un:
			case Code.Ble:
			case Code.Ble_Un:
			case Code.Blt:
			case Code.Blt_Un:
			case Code.Bne_Un:
			case Code.Br:
			case Code.Brfalse:
			case Code.Brtrue:
			case Code.Endfilter:
			case Code.Endfinally:
			case Code.Leave:
			case Code.Ret:
			case Code.Rethrow:
			case Code.Switch:
			case Code.Throw:
				break;

			case Code.Call:
			case Code.Calli:
			case Code.Callvirt:
				break;

			case Code.Arglist:
			case Code.Cpblk:
			case Code.Cpobj:
			case Code.Initblk:
			case Code.Initobj:
			case Code.Localloc:
			case Code.Mkrefany:
			case Code.Refanytype:
			case Code.Refanyval:
			case Code.Sizeof:
				break;

			case Code.Nop:
			case Code.Break:
			case Code.Unaligned:
			case Code.Volatile:
			case Code.Tailcall:
			case Code.Readonly:
				break;

			default:
				throw new NotSupportedException($"{instruction} is not supported");
			}
		}
	}
}
