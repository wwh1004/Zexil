using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using Zexil.DotNet.Emulation.Internal;

namespace Zexil.DotNet.Emulation.Emit {
	unsafe partial class Interpreter {
		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private void InterpretImpl(Instruction instruction, InterpreterMethodContext methodContext) {
			switch (instruction.OpCode.Code) {
			#region Arithmetic
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
			#endregion

			#region Comparison
			case Code.Ceq:
			case Code.Cgt:
			case Code.Cgt_Un:
			case Code.Ckfinite:
			case Code.Clt:
			case Code.Clt_Un:
				throw new NotImplementedException();
			#endregion

			#region Casting
			case Code.Box: {
				ref var valueSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsValueType && IsValueTypeStackNormalized(type));
#endif
				nint source = GetValueTypeAddress(ref valueSlot);
				valueSlot.I = Box(source, type, methodContext);
				valueSlot.ElementType = ElementType.Class;
				break;
			}
			case Code.Unbox: {
				ref var valueSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass);
#endif
				valueSlot.I += sizeof(nint);
				valueSlot.ElementType = ElementType.I;
				break;
			}
			case Code.Unbox_Any: {
				ref var valueSlot = ref methodContext.Peek();
				if (valueSlot.IsValueType) {
					// we should use TypeDesc.IsValueType for properly implementing unbox.any, but for performance we use InterpreterSlot.IsValueType
#if DEBUG
					System.Diagnostics.Debug.Assert(!valueSlot.IsByRef && !valueSlot.IsTypedRef && IsValueTypeStackNormalized(ResolveType(instruction.Operand, methodContext)));
#endif
					valueSlot.I += sizeof(nint);
					valueSlot.ElementType = ElementType.I;
					goto case Code.Ldobj;
				}
				else {
					goto case Code.Castclass;
				}
			}
			case Code.Castclass: {
				ref var valueSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass && IsClassStackNormalized(type));
#endif
				object value = PeekObject(methodContext);
				if (!type.IsInstanceOfType(value))
					throw new InvalidCastException($"Unable to cast object of type '{value.GetType()}' to type '{type}'.");
				break;
			}
			case Code.Isinst: {
				ref var valueSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass && IsClassStackNormalized(type));
#endif
				if (!type.IsInstanceOfType(PeekObject(methodContext)))
					valueSlot.I = 0;
				break;
			}
			case Code.Constrained: {
				ref var addressSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				// TODO
				System.Diagnostics.Debug.Assert(false);
				throw new NotImplementedException();
			}
			#endregion

			#region Conversion
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
				throw new NotImplementedException();
			#endregion

			#region Assignment
			case Code.Dup: {
				ref var slot = ref methodContext.Peek();
				methodContext.Push(slot);
				break;
			}
			case Code.Pop: {
				methodContext.Pop();
				break;
			}
			case Code.Ldarg: {
				int index = ResolveVariableIndex(instruction.Operand);
				ref var valueSlot = ref methodContext.Arguments[index];
				methodContext.Push(valueSlot);
				break;
			}
			case Code.Ldarga: {
				int index = ResolveVariableIndex(instruction.Operand);
				ref var valueSlot = ref methodContext.Arguments[index];
				var type = methodContext.ArgumentTypes[index];
				if (IsValueTypeStackNormalized(type) && !IsSlotSatisfied(type)) // already pointer to value type
					methodContext.PushI(valueSlot.I);
				else // direct stored value type or reference type
					methodContext.PushI((nint)Unsafe.AsPointer(ref valueSlot));
				break;
			}
			case Code.Ldc_I4: {
				methodContext.PushI4((int)instruction.Operand);
				break;
			}
			case Code.Ldc_I8: {
				methodContext.PushI8((long)instruction.Operand);
				break;
			}
			case Code.Ldc_R4: {
				methodContext.PushR4((float)instruction.Operand);
				break;
			}
			case Code.Ldc_R8: {
				methodContext.PushR8((double)instruction.Operand);
				break;
			}
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
				throw new NotImplementedException();
			case Code.Ldftn: {
				var method = ResolveMethod(instruction.Operand, methodContext);
				methodContext.PushI(method.GetMethodAddress());
				break;
			}
			case Code.Ldind_I: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I = *(nint*)addressSlot.I;
				addressSlot.ElementType = ElementType.I;
				break;
			}
			case Code.Ldind_I1:
			case Code.Ldind_U1: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I4 = *(sbyte*)addressSlot.I;
				addressSlot.ElementType = ElementType.I4;
				break;
			}
			case Code.Ldind_I2:
			case Code.Ldind_U2: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I4 = *(short*)addressSlot.I;
				addressSlot.ElementType = ElementType.I4;
				break;
			}
			case Code.Ldind_I4:
			case Code.Ldind_U4: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I4 = *(int*)addressSlot.I;
				addressSlot.ElementType = ElementType.I4;
				break;
			}
			case Code.Ldind_I8: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I8 = *(long*)addressSlot.I;
				addressSlot.ElementType = ElementType.I8;
				break;
			}
			case Code.Ldind_R4: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.R4 = *(float*)addressSlot.I;
				addressSlot.ElementType = ElementType.R4;
				break;
			}
			case Code.Ldind_R8: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.R8 = *(double*)addressSlot.I;
				addressSlot.ElementType = ElementType.R8;
				break;
			}
			case Code.Ldind_Ref: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I = *(nint*)addressSlot.I;
				addressSlot.ElementType = ElementType.Class;
				break;
			}
			case Code.Ldlen: {
				ref var valueSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass);
#endif
				valueSlot.I = *(nint*)(valueSlot.I + sizeof(nint));
				valueSlot.ElementType = ElementType.I;
				break;
			}
			case Code.Ldloc: {
				int index = ResolveVariableIndex(instruction.Operand);
				ref var valueSlot = ref methodContext.Locals[index];
				methodContext.Push(valueSlot);
				break;
			}
			case Code.Ldloca: {
				int index = ResolveVariableIndex(instruction.Operand);
				ref var valueSlot = ref methodContext.Locals[index];
				var type = methodContext.LocalTypes[index];
				if (IsValueTypeStackNormalized(type) && !IsSlotSatisfied(type)) // already pointer to value type
					methodContext.PushI(valueSlot.I);
				else // direct stored value type or reference type
					methodContext.PushI((nint)Unsafe.AsPointer(ref valueSlot));
				break;
			}
			case Code.Ldnull: {
				methodContext.Push(0, ElementType.Class);
				break;
			}
			case Code.Ldobj: {
				ref var addressSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert((addressSlot.IsI || addressSlot.IsByRef) && IsValueTypeStackNormalized(type));
#endif
				Ldobj(addressSlot.I, ref addressSlot, type, methodContext);
				break;
			}
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
				throw new NotImplementedException();
			case Code.Stind_I: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(nint*)addressSlot.I = valueSlot.I;
				break;
			}
			case Code.Stind_I1: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI4 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(int*)addressSlot.I = valueSlot.I4;
				break;
			}
			case Code.Stind_I2: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI4 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(int*)addressSlot.I = valueSlot.I4;
				break;
			}
			case Code.Stind_I4: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI4 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(int*)addressSlot.I = valueSlot.I4;
				break;
			}
			case Code.Stind_I8: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI8 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(long*)addressSlot.I = valueSlot.I8;
				break;
			}
			case Code.Stind_R4: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsR4 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(float*)addressSlot.I = valueSlot.R4;
				break;
			}
			case Code.Stind_R8: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsR8 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(double*)addressSlot.I = valueSlot.R8;
				break;
			}
			case Code.Stind_Ref: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(nint*)addressSlot.I = valueSlot.I;
				break;
			}
			case Code.Stloc: {
				ref var valueSlot = ref methodContext.Pop();
				int index = ResolveVariableIndex(instruction.Operand);
				methodContext.Locals[index] = valueSlot;
				break;
			}
			case Code.Stobj: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsValueType && (addressSlot.IsI || addressSlot.IsByRef) && IsValueTypeStackNormalized(type));
#endif
				nint source = GetValueTypeAddress(ref valueSlot);
				CopyValueType(source, addressSlot.I, type);
				break;
			}
			case Code.Stsfld:
				throw new NotImplementedException();
			#endregion

			#region Branch
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
				throw new NotImplementedException();
			case Code.Ret: {
				var method = methodContext.Method;
				if (!method.HasReturnType)
					break;
				ref var slot = ref methodContext.Pop();
				nint returnBuffer = methodContext.ReturnBuffer.I;
				// buffer to receive return value
				if (slot.IsValueType) {
					var returnType = method.ReturnType;
#if DEBUG
					System.Diagnostics.Debug.Assert(IsValueTypeStackNormalized(returnType));
#endif
					nint source = GetValueTypeAddress(ref slot);
					CopyValueType(source, returnBuffer, returnType);
				}
				else {
#if DEBUG
					System.Diagnostics.Debug.Assert(IsClassStackNormalized(method.ReturnType));
#endif
					*(nint*)returnBuffer = slot.I;
				}
				break;
			}
			case Code.Rethrow:
			case Code.Switch:
			case Code.Throw:
				throw new NotImplementedException();
			#endregion

			#region Calling
			case Code.Call:
			case Code.Calli:
			case Code.Callvirt:
				throw new NotImplementedException();
			#endregion

			#region Miscellaneous
			case Code.Arglist:
			case Code.Cpblk:
			case Code.Cpobj:
			case Code.Initblk:
			case Code.Initobj:
			case Code.Localloc:
			case Code.Mkrefany:
			case Code.Refanytype:
			case Code.Refanyval:
				throw new NotImplementedException();
			case Code.Sizeof: {
				int size = ResolveType(instruction.Operand, methodContext).Size;
				methodContext.PushI4(size);
				break;
			}
			#endregion

			#region Skipped
			case Code.Nop:
			case Code.Break:
			case Code.Unaligned:
			case Code.Volatile:
			case Code.Tailcall:
			case Code.Readonly:
				break;
			#endregion

			default:
				throw new NotSupportedException($"{instruction} is not supported");
			}
		}

		#region Value Type Methods
		/*   **********************
		 *        Introduction
		 *   **********************
		 *
		 *           Ldobj
		 *             ↓
		 *       StoreValueType -> IsSlotSatisfied -> CopyValueTypeNoGC
		 *             ↓
		 *            Box
		 *             ↓
		 * PushObject and CopyValueType -> CopyValueTypeNoGC
		 *
		 *   **********************
		 *        Introduction
		 *   **********************
		 */

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint GetValueTypeAddress(ref InterpreterSlot slot) {
			return IsSlotSatisfied(slot) ? (nint)Unsafe.AsPointer(ref slot) : slot.I;
		}

		/// <summary>
		/// We can direct store small value type in long type (8 bytes) only if it is unmanaged, otherwise we should use a large container with GC support
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsSlotSatisfied(TypeDesc type) {
			return !type.IsLargeValueType && type.IsUnmanaged;
		}

		/// <summary>
		/// We can direct store small value type in long type (8 bytes) only if it is unmanaged, otherwise we should use a large container with GC support
		/// </summary>
		/// <param name="slot"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsSlotSatisfied(in InterpreterSlot slot) {
			return !slot.AnnotatedElementType.IsLargeValueType() && slot.AnnotatedElementType.IsUnmanaged();
		}

		/// <summary>
		/// Ldobj
		/// </summary>
		/// <param name="source"></param>
		/// <param name="slot"></param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Ldobj(nint source, ref InterpreterSlot slot, TypeDesc type, InterpreterMethodContext methodContext) {
			StoreValueType(source, (nint)Unsafe.AsPointer(ref slot), type, methodContext);
			slot.ElementType = StackNormalize(type.ElementType);
			slot.Annotation |= type.Annotation;
		}

		/// <summary>
		/// Store a value type in "long" type
		/// </summary>
		/// <param name="source">Value type address</param>
		/// <param name="destination"><see cref="InterpreterSlot"/> address</param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void StoreValueType(nint source, nint destination, TypeDesc type, InterpreterMethodContext methodContext) {
			if (IsSlotSatisfied(type))
				CopyValueTypeNoGC(source, destination, type.Size);
			else
				*(nint*)destination = Box(source, type, methodContext) + sizeof(nint);
		}

		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void StoreValueTypeAligned(nint source, nint destination, TypeDesc type, InterpreterMethodContext methodContext) {
			if (IsSlotSatisfied(type))
				CopyValueTypeNoGCAligned(source, destination, type.AlignedSize);
			else
				*(nint*)destination = Box(source, type, methodContext) + sizeof(nint);
		}

		/// <summary>
		/// Box
		/// </summary>
		/// <param name="source">Value type address</param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint Box(nint source, TypeDesc type, InterpreterMethodContext methodContext) {
			object boxedValue = GCHelpers.AllocateObject(type.TypeHandle);
			nint objectRef = PushObject(boxedValue, methodContext);
			CopyValueType(source, objectRef + sizeof(nint), type);
			return objectRef;
		}

		/// <summary>
		/// Copy value type
		/// </summary>
		/// <param name="source">Source address</param>
		/// <param name="destination">Destination address</param>
		/// <param name="type"></param>
		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void CopyValueType(nint source, nint destination, TypeDesc type) {
			int size = type.Size;
		loop:
			CopyValueTypeNoGC(source, destination, size);
			if (!type.IsUnmanaged && !Memcmp(source, destination, size)) {
				System.Diagnostics.Debug.Assert(false, "GC moves field(s) when coping value type");
				goto loop;
			}
			// We should verify whether GC moves managed fields
		}

		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void CopyValueTypeAligned(nint source, nint destination, TypeDesc type) {
			int size = type.AlignedSize;
		loop:
			CopyValueTypeNoGCAligned(source, destination, size);
			if (!type.IsUnmanaged && !Memcmp(source, destination, size)) {
				System.Diagnostics.Debug.Assert(false, "GC moves field(s) when coping value type");
				goto loop;
			}
			// We should verify whether GC moves managed fields
		}

		/// <summary>
		/// Inlined fast copy value type
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="size"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyValueTypeNoGC(nint source, nint destination, int size) {
			if (size == 8)
				*(ulong*)destination = *(ulong*)source;
			else if (size == 4)
				*(uint*)destination = *(uint*)source;
			else if (size == 2)
				*(ushort*)destination = *(ushort*)source;
			else if (size == 1)
				*(byte*)destination = *(byte*)source;
			else
				Memcpy(source, destination, size);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void CopyValueTypeNoGCAligned(nint source, nint destination, int size) {
			if (size == 8) {
				*(ulong*)destination = *(ulong*)source;
			}
			else if (size == 4) {
				*(uint*)destination = *(uint*)source;
			}
			else {
				System.Diagnostics.Debug.Assert(size > 8);
				// we use aligned size so size shouldn't be sizeof(byte), sizeof(short) and other values
				Memcmp(source, destination, size);
			}
		}
		#endregion

		#region Stack Normalizing
		private static bool IsClassStackNormalized(TypeDesc type) {
#if DEBUG
			System.Diagnostics.Debug.Assert(StackNormalize(type.ElementType) == ElementType.Class == (!type.IsValueType && !type.IsPointer && !type.IsByRef));
#endif
			return StackNormalize(type.ElementType) == ElementType.Class;
		}

		private static bool IsValueTypeStackNormalized(TypeDesc type) {
#if DEBUG
			System.Diagnostics.Debug.Assert(StackNormalize(type.ElementType) != ElementType.Class == (type.IsValueType || type.IsPointer || type.IsByRef));
#endif
			return StackNormalize(type.ElementType) != ElementType.Class;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0066:Convert switch statement to expression", Justification = "<Pending>")]
		private static ElementType StackNormalize(ElementType elementType) {
			switch (elementType) {
			case ElementType.I4:
			case ElementType.I8:
			case ElementType.R4:
			case ElementType.R8:
			case ElementType.ByRef:
			case ElementType.ValueType:
			case ElementType.Class:
			case ElementType.TypedByRef:
			case ElementType.I:
				return elementType;

			case ElementType.Boolean:
			case ElementType.Char:
			case ElementType.I1:
			case ElementType.U1:
			case ElementType.I2:
			case ElementType.U2:
			case ElementType.U4:
				return ElementType.I4;

			case ElementType.U8:
				return ElementType.I8;

			case ElementType.Ptr:
			case ElementType.U:
			case ElementType.FnPtr:
				return ElementType.I;

			case ElementType.String:
			case ElementType.Array:
			case ElementType.Object:
			case ElementType.SZArray:
				return ElementType.Class;

			default:
				throw new InvalidProgramException();
			}
		}
		#endregion

		#region Variable Helpers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal static InterpreterSlot[] ConvertArguments(nint[] stubArguments, InterpreterMethodContext methodContext) {
			var arguments = new InterpreterSlot[stubArguments.Length];
			for (int i = 0; i < methodContext.Method.Parameters.Length; i++) {
				var type = methodContext.ArgumentTypes[i];
				if (type.IsValueType) {
					// do not use stack-normalized api, interpreter stub doesn't regard pointer and byref as value type
					Ldobj(stubArguments[i], ref arguments[i], type, methodContext);
				}
				else {
					arguments[i].I = stubArguments[i];
					arguments[i].AnnotatedElementType = (AnnotatedElementType)StackNormalize(type.ElementType) | type.Annotation;
				}
			}
			if (methodContext.Method.HasReturnType) {
				ref var returnBuffer = ref arguments[arguments.Length - 1];
				returnBuffer.I = stubArguments[stubArguments.Length - 1];
				returnBuffer.ElementType = ElementType.I;
			}
			return arguments;
		}
		#endregion

		#region Dnlib Helpers
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static TypeDesc ResolveType(object operand, InterpreterMethodContext methodContext) {
			return methodContext.Module.ResolveType(((IMDTokenProvider)operand).MDToken.ToInt32(), methodContext.TypeInstantiation, methodContext.MethodInstantiation);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static MethodDesc ResolveMethod(object operand, InterpreterMethodContext methodContext) {
			return methodContext.Module.ResolveMethod(((IMDTokenProvider)operand).MDToken.ToInt32(), methodContext.TypeInstantiation, methodContext.MethodInstantiation);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static FieldDesc ResolveField(object operand, InterpreterMethodContext methodContext) {
			return methodContext.Module.ResolveField(((IMDTokenProvider)operand).MDToken.ToInt32(), methodContext.TypeInstantiation, methodContext.MethodInstantiation);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static int ResolveVariableIndex(object operand) {
			return ((IVariable)operand).Index;
		}
		#endregion

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint PushObject(object value, InterpreterMethodContext methodContext) {
			nint objectRef = PinObject(value, methodContext);
			methodContext.Push(objectRef, ElementType.Class);
			return objectRef;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static object PopObject(InterpreterMethodContext methodContext) {
			nint objectRef = methodContext.Pop().I;
			object value = Unsafe.AsRef<object>(&objectRef);
			TryUnpinObject(methodContext);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static object PeekObject(InterpreterMethodContext methodContext) {
			nint objectRef = methodContext.Pop().I;
			object value = Unsafe.AsRef<object>(&objectRef);
			return value;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint PinObject(object value, InterpreterMethodContext methodContext) {
			var handle = GCHandle.Alloc(value, GCHandleType.Pinned);
			methodContext.Handles.Push(handle);
			methodContext.LastUsedHandle = handle;
			return handle.AddrOfPinnedObject();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void TryUnpinObject(InterpreterMethodContext methodContext) {
			if (methodContext.LastUsedHandle == methodContext.Handles.Peek()) {
				methodContext.LastUsedHandle = default;
				var handle = methodContext.Handles.Pop();
				handle.Free();
			}
		}

		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void Memcpy(nint source, nint destination, int length) {
			int offset = 0;
			while (length >= sizeof(nint)) {
				*(nint*)(destination + offset) = *(nint*)(source + offset);
				length -= sizeof(nint);
				offset += sizeof(nint);
			}
			for (; offset < length; offset++)
				*(byte*)(destination + offset) = *(byte*)(source + offset);
		}

		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static bool Memcmp(nint source, nint destination, int length) {
			int offset = 0;
			while (length >= sizeof(nint)) {
				if (*(nint*)(destination + offset) != *(nint*)(source + offset))
					return false;
				length -= sizeof(nint);
				offset += sizeof(nint);
			}
			for (; offset < length; offset++) {
				if (*(byte*)(destination + offset) != *(byte*)(source + offset))
					return false;
			}
			return true;
		}

		[MethodImpl(512 /*MethodImplOptions.AggressiveOptimization*/)]
		private static void Memset(nint source/*,int value*/, int length) {
			int offset = 0;
			while (length >= sizeof(nint)) {
				*(nint*)(source + offset) = 0;
				length -= sizeof(nint);
				offset += sizeof(nint);
			}
			for (; offset < length; offset++)
				*(byte*)(source + offset) = 0;
		}
	}
}
