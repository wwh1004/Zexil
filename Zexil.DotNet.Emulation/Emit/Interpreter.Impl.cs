using System;
using System.Collections.Generic;
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
			#endregion

			#region Comparison
			case Code.Ckfinite: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.R4:
					if ((valueSlot.I4 & 0x7FFFFFFF) > 0x7F800000)
						throw new OverflowException();
					break;
				case ElementType.R8:
					if ((valueSlot.I8 & 0x7FFFFFFFFFFFFFFF) >= 0x7FF0000000000000)
						throw new OverflowException();
					break;
				default:
					throw new InvalidProgramException("CkFinite requires a floating-point value on the stack.");
				}
				// From CoreCLR:
				// According to the ECMA spec, this should be an ArithmeticException; however,
				// the JITs throw an OverflowException and consistency is top priority...
				break;
			}
			case Code.Ceq: {
				bool result = Ceq(methodContext);
				methodContext.PushI4(result ? 1 : 0);
				break;
			}
			case Code.Cgt: {
				bool result = Cgt(methodContext);
				methodContext.PushI4(result ? 1 : 0);
				break;
			}
			case Code.Cgt_Un: {
				bool result = CgtUn(methodContext);
				methodContext.PushI4(result ? 1 : 0);
				break;
			}
			case Code.Clt: {
				bool result = Clt(methodContext);
				methodContext.PushI4(result ? 1 : 0);
				break;
			}
			case Code.Clt_Un: {
				bool result = CltUn(methodContext);
				methodContext.PushI4(result ? 1 : 0);
				break;
			}
			#endregion

			#region Casting
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
			case Code.Unbox: {
				ref var valueSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass);
#endif
				valueSlot.I += sizeof(nint);
				valueSlot.ElementType = ElementType.I;
				break;
			}
			case Code.Box: {
				ref var valueSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsValueType && IsValueTypeStackNormalized(type));
#endif
				valueSlot.I = Box(ref valueSlot, type, methodContext);
				valueSlot.ElementType = ElementType.Class;
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
			case Code.Constrained: {
				ref var addressSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				if (IsClassStackNormalized(type)) {
					addressSlot.I = *(nint*)addressSlot.I;
					SetAnnotatedElementType(ref addressSlot, type);
				}
				else {
					methodContext.IsConstrainedValueType = true;
				}
				break;
			}
			#endregion

			#region Conversion
			case Code.Conv_I1:
			case Code.Conv_I2:
			case Code.Conv_I4:
			case Code.Conv_U4:
			case Code.Conv_U2:
			case Code.Conv_U1: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, (int)valueSlot.I8);
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, (int)valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, (int)valueSlot.R4);
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, (int)valueSlot.R8);
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.* operation.");
				}
				break;
			}
			case Code.Conv_I8:
			case Code.Conv_U8: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI8(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI8(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI8(ref valueSlot, (long)valueSlot.R4);
					break;
				case ElementType.R8:
					ConvertI8(ref valueSlot, (long)valueSlot.R8);
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.* operation.");
				}
				break;
			}
			case Code.Conv_R4: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertR4(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					ConvertR4(ref valueSlot, valueSlot.I8);
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertR4(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					break;
				case ElementType.R8:
					ConvertR4(ref valueSlot, (float)valueSlot.R8);
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.* operation.");
				}
				break;
			}
			case Code.Conv_R8: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertR8(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					ConvertR8(ref valueSlot, valueSlot.I8);
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertR8(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertR8(ref valueSlot, (float)valueSlot.R8);
					break;
				case ElementType.R8:
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.* operation.");
				}
				break;
			}
			case Code.Conv_R_Un: {
				// see coreclr 'void Interpreter::ConvRUn()'
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertR8(ref valueSlot, valueSlot.U4);
					break;
				case ElementType.I8:
					ConvertR8(ref valueSlot, valueSlot.U8);
					break;
				case ElementType.I:
					// roslyn doesn't emit conv.r.un for nuint
					if (sizeof(nint) == 4)
						goto case ElementType.I4;
					else
						goto case ElementType.I8;
				case ElementType.R8:
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.r.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I1_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I2_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((short)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((short)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((short)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((short)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((short)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I4_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((int)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((int)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((int)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((int)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((int)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I8_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI8(ref valueSlot, valueSlot.U4);
					break;
				case ElementType.I8:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U1_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U2_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U4_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U8_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI8(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI8(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI(ref valueSlot, checked((nint)valueSlot.U4));
					break;
				case ElementType.I8:
					ConvertI(ref valueSlot, checked((nint)valueSlot.U8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI(ref valueSlot, checked((nint)valueSlot.U));
					break;
				case ElementType.R4:
					ConvertI(ref valueSlot, checked((nint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI(ref valueSlot, checked((nint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U_Un: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					break;
				case ElementType.I8:
					ConvertI(ref valueSlot, (nint)checked((nuint)valueSlot.U8));
					break;
				case ElementType.I:
					break;
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI(ref valueSlot, (nint)valueSlot.U);
					break;
				case ElementType.R4:
					ConvertI(ref valueSlot, (nint)checked((nuint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI(ref valueSlot, (nint)checked((nuint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.*.un operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I1: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((sbyte)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U1: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((byte)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I2: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((short)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((short)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((short)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((short)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((short)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U2: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((ushort)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I4: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, checked((int)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, checked((int)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, checked((int)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, checked((int)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U4: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI4(ref valueSlot, (int)checked((uint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I8: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI8(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI8(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI8(ref valueSlot, (int)checked((long)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U8: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI8(ref valueSlot, (long)checked((ulong)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_I:
			case Code.Conv_U: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					ConvertI(ref valueSlot, (nint)valueSlot.I8);
					break;
				case ElementType.I:
					break;
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI(ref valueSlot, (nint)valueSlot.R4);
					break;
				case ElementType.R8:
					ConvertI(ref valueSlot, (nint)valueSlot.R8);
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_I: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI(ref valueSlot, valueSlot.I4);
					break;
				case ElementType.I8:
					ConvertI(ref valueSlot, checked((nint)valueSlot.I8));
					break;
				case ElementType.I:
					break;
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI(ref valueSlot, valueSlot.I);
					break;
				case ElementType.R4:
					ConvertI(ref valueSlot, checked((nint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI(ref valueSlot, checked((nint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			case Code.Conv_Ovf_U: {
				ref var valueSlot = ref methodContext.Peek();
				switch (valueSlot.ElementType) {
				case ElementType.I4:
					ConvertI(ref valueSlot, (int)checked((nuint)valueSlot.I4));
					break;
				case ElementType.I8:
					ConvertI(ref valueSlot, (int)checked((nuint)valueSlot.I8));
					break;
				case ElementType.I:
				case ElementType.ByRef:
				case ElementType.Class:
					ConvertI(ref valueSlot, (int)checked((nuint)valueSlot.I));
					break;
				case ElementType.R4:
					ConvertI(ref valueSlot, (int)checked((nuint)valueSlot.R4));
					break;
				case ElementType.R8:
					ConvertI(ref valueSlot, (int)checked((nuint)valueSlot.R8));
					break;
				default:
					throw new InvalidProgramException("Illegal operand type for conv.ovf.* operation.");
				}
				break;
			}
			#endregion

			#region Assignment
			case Code.Ldnull: {
				methodContext.Push(0, ElementType.Class);
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
			case Code.Dup: {
				ref var slot = ref methodContext.Peek();
				methodContext.Push(slot);
				break;
			}
			case Code.Pop: {
				methodContext.Pop();
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
			case Code.Ldind_I: {
				ref var addressSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(addressSlot.IsI || addressSlot.IsByRef);
#endif
				addressSlot.I = *(nint*)addressSlot.I;
				addressSlot.ElementType = ElementType.I;
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
			case Code.Stind_Ref: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass && (addressSlot.IsI || addressSlot.IsByRef));
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
				*(sbyte*)addressSlot.I = (sbyte)valueSlot.I4;
				break;
			}
			case Code.Stind_I2: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI4 && (addressSlot.IsI || addressSlot.IsByRef));
#endif
				*(short*)addressSlot.I = (short)valueSlot.I4;
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
			case Code.Ldobj: {
				ref var addressSlot = ref methodContext.Peek();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert((addressSlot.IsI || addressSlot.IsByRef) && IsValueTypeStackNormalized(type));
#endif
				Ldobj(addressSlot.I, ref addressSlot, type, methodContext);
				break;
			}
			case Code.Ldstr:
				throw new NotImplementedException();
			case Code.Newobj:
				throw new NotImplementedException();
			case Code.Ldfld:
				throw new NotImplementedException();
			case Code.Ldflda:
				throw new NotImplementedException();
			case Code.Stfld:
				throw new NotImplementedException();
			case Code.Ldsfld:
				throw new NotImplementedException();
			case Code.Ldsflda:
				throw new NotImplementedException();
			case Code.Stsfld:
				throw new NotImplementedException();
			case Code.Stobj: {
				ref var valueSlot = ref methodContext.Pop();
				ref var addressSlot = ref methodContext.Pop();
				var type = ResolveType(instruction.Operand, methodContext);
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsValueType && (addressSlot.IsI || addressSlot.IsByRef) && IsValueTypeStackNormalized(type));
#endif
				Stobj(ref valueSlot, addressSlot.I, type);
				break;
			}
			case Code.Newarr:
				throw new NotImplementedException();
			case Code.Ldlen: {
				ref var valueSlot = ref methodContext.Peek();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsClass);
#endif
				valueSlot.I = *(nint*)(valueSlot.I + sizeof(nint));
				valueSlot.ElementType = ElementType.I;
				break;
			}
			case Code.Ldelema: {
				var type = ResolveType(instruction.Operand, methodContext);
				nint address = Ldelema(type.Size, methodContext);
				methodContext.PushByRef(address);
				break;
			}
			case Code.Ldelem_I1:
			case Code.Ldelem_U1: {
				nint address = Ldelema(1, methodContext);
				methodContext.PushI4(*(sbyte*)address);
				break;
			}
			case Code.Ldelem_I2:
			case Code.Ldelem_U2: {
				nint address = Ldelema(2, methodContext);
				methodContext.PushI4(*(short*)address);
				break;
			}
			case Code.Ldelem_I4:
			case Code.Ldelem_U4: {
				nint address = Ldelema(4, methodContext);
				methodContext.PushI4(*(int*)address);
				break;
			}
			case Code.Ldelem_I8: {
				nint address = Ldelema(8, methodContext);
				methodContext.PushI8(*(long*)address);
				break;
			}
			case Code.Ldelem_I: {
				nint address = Ldelema(sizeof(nint), methodContext);
				methodContext.PushI(*(nint*)address);
				break;
			}
			case Code.Ldelem_R4: {
				nint address = Ldelema(4, methodContext);
				methodContext.PushR4(*(float*)address);
				break;
			}
			case Code.Ldelem_R8: {
				nint address = Ldelema(8, methodContext);
				methodContext.PushR8(*(double*)address);
				break;
			}
			case Code.Ldelem_Ref: {
				nint address = Ldelema(sizeof(nint), methodContext);
				nint objectRef = *(nint*)address;
				PushObject(objectRef, methodContext);
				// TODO: when reference type has annotation, we should use PushObject with 3 arguments
				break;
			}
			case Code.Stelem_I: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(8, methodContext);
				*(nint*)address = valueSlot.I;
				break;
			}
			case Code.Stelem_I1: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(1, methodContext);
				*(sbyte*)address = (sbyte)valueSlot.I4;
				break;
			}
			case Code.Stelem_I2: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(2, methodContext);
				*(short*)address = (short)valueSlot.I4;
				break;
			}
			case Code.Stelem_I4: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(4, methodContext);
				*(int*)address = valueSlot.I4;
				break;
			}
			case Code.Stelem_I8: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(8, methodContext);
				*(long*)address = valueSlot.I8;
				break;
			}
			case Code.Stelem_R4: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(4, methodContext);
				*(float*)address = valueSlot.R4;
				break;
			}
			case Code.Stelem_R8: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(8, methodContext);
				*(double*)address = valueSlot.R8;
				break;
			}
			case Code.Stelem_Ref: {
				ref var valueSlot = ref methodContext.Pop();
				nint address = Ldelema(sizeof(nint), methodContext);
				*(nint*)address = valueSlot.I;
				TryUnpinObject(methodContext);
				break;
			}
			case Code.Ldelem: {
				var type = ResolveType(instruction.Operand, methodContext);
				nint address = Ldelema(type.Size, methodContext);
				ref var valueSlot = ref methodContext.Push();
				LoadAny(address, ref valueSlot, type, methodContext);
				break;
			}
			case Code.Stelem: {
				ref var valueSlot = ref methodContext.Pop();
				var type = ResolveType(instruction.Operand, methodContext);
				nint address = Ldelema(type.Size, methodContext);
				SetAny(ref valueSlot, address, type);
				break;
			}
			case Code.Ldtoken:
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
			case Code.Ldftn: {
				var method = ResolveMethod(instruction.Operand, methodContext);
				methodContext.PushI(method.GetMethodAddress());
				break;
			}
			case Code.Ldvirtftn:
				throw new NotImplementedException();
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
			case Code.Starg: {
				ref var valueSlot = ref methodContext.Pop();
				int index = ResolveVariableIndex(instruction.Operand);
				methodContext.Arguments[index] = valueSlot;
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
			case Code.Stloc: {
				ref var valueSlot = ref methodContext.Pop();
				int index = ResolveVariableIndex(instruction.Operand);
				methodContext.Locals[index] = valueSlot;
				break;
			}
			#endregion

			#region Branch
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
					Stobj(ref slot, returnBuffer, returnType);
				}
				else {
#if DEBUG
					System.Diagnostics.Debug.Assert(IsClassStackNormalized(method.ReturnType));
#endif
					*(nint*)returnBuffer = slot.I;
				}
				break;
			}
			case Code.Br: {
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Brfalse: {
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if ((byte)methodContext.Pop().I4 == 0)
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Brtrue: {
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if ((byte)methodContext.Pop().I4 != 0)
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Beq: {
				// The effect is the same as performing a ceq instruction followed by a brtrue branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if (Ceq(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Bge: {
				// The effect is identical to performing a clt instruction (clt.un for floats) followed by a brfalse branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (!((!v2.IsR4 && !v2.IsR8) ? Clt(methodContext) : CltUn(methodContext)))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Bgt: {
				// The effect is identical to performing a cgt instruction followed by a brtrue branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if (Cgt(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Ble: {
				// The effect is identical to performing a cgt instruction (cgt.un for floats) followed by a brfalse branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (!((!v2.IsR4 && !v2.IsR8) ? Cgt(methodContext) : CgtUn(methodContext)))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Blt: {
				// The effect is identical to performing a clt instruction followed by a brtrue branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if (Clt(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Bne_Un: {
				// The effect is identical to performing a ceq instruction followed by a brfalse branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				if (!Ceq(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Bge_Un: {
				// The effect is identical to performing a clt.un instruction (clt for floats) followed by a brfalse branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (!((!v2.IsR4 && !v2.IsR8) ? CltUn(methodContext) : Clt(methodContext)))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Bgt_Un: {
				// The effect is identical to performing a cgt.un instruction followed by a brtrue branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (CgtUn(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Ble_Un: {
				// The effect is identical to performing a cgt.un instruction (cgt for floats) followed by a brfalse branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (!((!v2.IsR4 && !v2.IsR8) ? CgtUn(methodContext) : Cgt(methodContext)))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Blt_Un: {
				// The effect is identical to performing a clt.un instruction followed by a brtrue branch to the specific target instruction.
#if DEBUG
				System.Diagnostics.Debug.Assert(methodContext.NextILOffset is null);
#endif
				ref var v2 = ref methodContext.Peek();
				if (CltUn(methodContext))
					methodContext.NextILOffset = ((Instruction)instruction.Operand).Offset;
				break;
			}
			case Code.Switch: {
				ref var valueSlot = ref methodContext.Pop();
#if DEBUG
				System.Diagnostics.Debug.Assert(valueSlot.IsI4 && methodContext.NextILOffset is null);
#endif
				int index = valueSlot.I4;
				var targets = (IList<Instruction>)instruction.Operand;
				if (index < 0 || index >= targets.Count)
					break;
				methodContext.NextILOffset = targets[index].Offset;
				break;
			}
			case Code.Throw: {
				object exception = PopObject(methodContext);
				// Maybe not instance of System.Exception, do not cast it
				throw Unsafe.As<object, Exception>(ref exception);
			}
			case Code.Endfinally:
				throw new NotImplementedException();
			case Code.Leave:
				goto case Code.Br;
			// TODO: implement exception handler
			case Code.Endfilter:
				throw new NotImplementedException();
			case Code.Rethrow:
				throw new NotImplementedException();
			#endregion

			#region Calling
			case Code.Call:
				throw new NotImplementedException();
			case Code.Calli:
				throw new NotImplementedException();
			case Code.Callvirt:
				throw new NotImplementedException();
			#endregion

			#region Miscellaneous
			case Code.Cpobj:
				throw new NotImplementedException();
			case Code.Refanyval:
				throw new NotImplementedException();
			case Code.Mkrefany:
				throw new NotImplementedException();
			case Code.Arglist:
				throw new NotImplementedException();
			case Code.Localloc:
				throw new NotImplementedException();
			case Code.Initobj:
				throw new NotImplementedException();
			case Code.Cpblk:
				throw new NotImplementedException();
			case Code.Initblk:
				throw new NotImplementedException();
			case Code.Sizeof: {
				int size = ResolveType(instruction.Operand, methodContext).Size;
				methodContext.PushI4(size);
				break;
			}
			case Code.Refanytype:
				throw new NotImplementedException();
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

		#region Value Type
		/*   **********************
		 *        Introduction
		 *   **********************
		 *
		 *           Ldobj
		 *             ↓
		 *       StoreValueType -> IsSlotSatisfied -> CopyValueTypeNoGC
		 *             ↓
		 *          BoxImpl
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
		/// <param name="destination"></param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Ldobj(nint source, ref InterpreterSlot destination, TypeDesc type, InterpreterMethodContext methodContext) {
			if (IsSlotSatisfied(type))
				CopyValueTypeNoGC(source, (nint)Unsafe.AsPointer(ref destination), type.Size);
			else
				*(nint*)Unsafe.AsPointer(ref destination) = BoxImpl(source, type, methodContext) + sizeof(nint);
			SetAnnotatedElementType(ref destination, type);
		}

		/// <summary>
		/// Box
		/// </summary>
		/// <param name="source">Value type address</param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint Box(ref InterpreterSlot source, TypeDesc type, InterpreterMethodContext methodContext) {
			return BoxImpl(GetValueTypeAddress(ref source), type, methodContext);
		}

		/// <summary>
		/// Box implementation
		/// </summary>
		/// <param name="source">Value type address</param>
		/// <param name="type"></param>
		/// <param name="methodContext"></param>
		/// <returns></returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint BoxImpl(nint source, TypeDesc type, InterpreterMethodContext methodContext) {
			object boxedValue = GCHelpers.AllocateObject(type.TypeHandle);
			nint objectRef = PushObject(boxedValue, methodContext);
			CopyValueType(source, objectRef + sizeof(nint), type);
			return objectRef;
		}

		/// <summary>
		/// Stobj
		/// </summary>
		/// <param name="source"></param>
		/// <param name="destination"></param>
		/// <param name="type"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void Stobj(ref InterpreterSlot source, nint destination, TypeDesc type) {
			CopyValueType(GetValueTypeAddress(ref source), destination, type);
		}

		/// <summary>
		/// Copy value type
		/// </summary>
		/// <param name="source">Source address</param>
		/// <param name="destination">Destination address</param>
		/// <param name="type"></param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
		#endregion

		#region Stack Normalizing
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsClassStackNormalized(TypeDesc type) {
#if DEBUG
			System.Diagnostics.Debug.Assert(StackNormalize(type.ElementType) == ElementType.Class == (!type.IsValueType && !type.IsPointer && !type.IsByRef));
#endif
			return StackNormalize(type.ElementType) == ElementType.Class;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool IsValueTypeStackNormalized(TypeDesc type) {
#if DEBUG
			System.Diagnostics.Debug.Assert(StackNormalize(type.ElementType) != ElementType.Class == (type.IsValueType || type.IsPointer || type.IsByRef));
#endif
			return StackNormalize(type.ElementType) != ElementType.Class;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetAnnotatedElementType(ref InterpreterSlot slot, TypeDesc type) {
			slot.AnnotatedElementType = (AnnotatedElementType)StackNormalize(type.ElementType) | type.Annotation;
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

		#region Comparison
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Ceq(InterpreterMethodContext methodContext) {
			ref var v2 = ref methodContext.Pop();
			ref var v1 = ref methodContext.Pop();
			switch (v1.ElementType) {
			case ElementType.I4:
				if (v2.IsI4)
					return v1.I4 == v2.I4;
				else if (v2.IsI8)
					return v1.I4 == v2.I8;
				else if (v2.IsI)
					return v1.I4 == v2.I;
				else if (v2.IsByRef)
					return v1.I4 == v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I8:
				if (v2.IsI8)
					return v1.I8 == v2.I8;
				else if (v2.IsI4)
					return v1.I8 == v2.I4;
				else if (v2.IsI)
					return v1.I8 == v2.I;
				else if (v2.IsByRef)
					return v1.I8 == v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I:
			case ElementType.ByRef:
			case ElementType.Class:
				if (v2.IsI)
					return v1.I == v2.I;
				else if (v2.IsByRef)
					return v1.I == v2.I;
				else if (v2.IsI4)
					return v1.I == v2.I4;
				else if (v2.IsI8)
					return v1.I == v2.I8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R4:
				if (v2.IsR4)
					return v1.R4 == v2.R4;
				else if (v2.IsR8)
					return v1.R4 == v2.R8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R8:
				if (v2.IsR8)
					return v1.R8 == v2.R8;
				else if (v2.IsR4)
					return v1.R8 == v2.R4;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			default:
				throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Cgt(InterpreterMethodContext methodContext) {
			ref var v2 = ref methodContext.Pop();
			ref var v1 = ref methodContext.Pop();
			switch (v1.ElementType) {
			case ElementType.I4:
				if (v2.IsI4)
					return v1.I4 > v2.I4;
				else if (v2.IsI8)
					return v1.I4 > v2.I8;
				else if (v2.IsI)
					return v1.I4 > v2.I;
				else if (v2.IsByRef)
					return v1.I4 > v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I8:
				if (v2.IsI8)
					return v1.I8 > v2.I8;
				else if (v2.IsI4)
					return v1.I8 > v2.I4;
				else if (v2.IsI)
					return v1.I8 > v2.I;
				else if (v2.IsByRef)
					return v1.I8 > v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I:
			case ElementType.ByRef:
				if (v2.IsI)
					return v1.I > v2.I;
				else if (v2.IsByRef)
					return v1.I > v2.I;
				else if (v2.IsI4)
					return v1.I > v2.I4;
				else if (v2.IsI8)
					return v1.I > v2.I8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R4:
				if (v2.IsR4)
					return v1.R4 > v2.R4;
				else if (v2.IsR8)
					return v1.R4 > v2.R8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R8:
				if (v2.IsR8)
					return v1.R8 > v2.R8;
				else if (v2.IsR4)
					return v1.R8 > v2.R4;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			default:
				throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CgtUn(InterpreterMethodContext methodContext) {
			ref var v2 = ref methodContext.Pop();
			ref var v1 = ref methodContext.Pop();
			switch (v1.ElementType) {
			case ElementType.I4:
				if (v2.IsI4)
					return v1.U4 > v2.U4;
				else if (v2.IsI8)
					return v1.U4 > v2.U8;
				else if (v2.IsI)
					return v1.U4 > v2.U;
				else if (v2.IsByRef)
					return v1.U4 > v2.U;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I8:
				if (v2.IsI8)
					return v1.U8 > v2.U8;
				else if (v2.IsI4)
					return v1.U8 > v2.U4;
				else if (v2.IsI)
					return v1.U8 > v2.U;
				else if (v2.IsByRef)
					return v1.U8 > v2.U;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I:
			case ElementType.ByRef:
			case ElementType.Class:
				// see coreclr interpreter implementation or https://stackoverflow.com/questions/28781839/why-does-the-c-sharp-compiler-translate-this-comparison-as-if-it-were-a-com
				if (v2.IsI)
					return v1.U > v2.U;
				else if (v2.IsByRef)
					return v1.U > v2.U;
				else if (v2.IsI4)
					return v1.U > v2.U4;
				else if (v2.IsI8)
					return v1.U > v2.U8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R4:
				if (v2.IsR4)
					return !(v1.R4 <= v2.R4);
				else if (v2.IsR8)
					return !(v1.R4 <= v2.R8);
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R8:
				if (v2.IsR8)
					return !(v1.R8 <= v2.R8);
				else if (v2.IsR4)
					return !(v1.R8 <= v2.R4);
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			default:
				throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool Clt(InterpreterMethodContext methodContext) {
			ref var v2 = ref methodContext.Pop();
			ref var v1 = ref methodContext.Pop();
			switch (v1.ElementType) {
			case ElementType.I4:
				if (v2.IsI4)
					return v1.I4 < v2.I4;
				else if (v2.IsI8)
					return v1.I4 < v2.I8;
				else if (v2.IsI)
					return v1.I4 < v2.I;
				else if (v2.IsByRef)
					return v1.I4 < v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I8:
				if (v2.IsI8)
					return v1.I8 < v2.I8;
				else if (v2.IsI4)
					return v1.I8 < v2.I4;
				else if (v2.IsI)
					return v1.I8 < v2.I;
				else if (v2.IsByRef)
					return v1.I8 < v2.I;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I:
			case ElementType.ByRef:
				if (v2.IsI)
					return v1.I < v2.I;
				else if (v2.IsByRef)
					return v1.I < v2.I;
				else if (v2.IsI4)
					return v1.I < v2.I4;
				else if (v2.IsI8)
					return v1.I < v2.I8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R4:
				if (v2.IsR4)
					return v1.R4 < v2.R4;
				else if (v2.IsR8)
					return v1.R4 < v2.R8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R8:
				if (v2.IsR8)
					return v1.R8 < v2.R8;
				else if (v2.IsR4)
					return v1.R8 < v2.R4;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			default:
				throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static bool CltUn(InterpreterMethodContext methodContext) {
			ref var v2 = ref methodContext.Pop();
			ref var v1 = ref methodContext.Pop();
			switch (v1.ElementType) {
			case ElementType.I4:
				if (v2.IsI4)
					return v1.U4 < v2.U4;
				else if (v2.IsI8)
					return v1.U4 < v2.U8;
				else if (v2.IsI)
					return v1.U4 < v2.U;
				else if (v2.IsByRef)
					return v1.U4 < v2.U;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I8:
				if (v2.IsI8)
					return v1.U8 < v2.U8;
				else if (v2.IsI4)
					return v1.U8 < v2.U4;
				else if (v2.IsI)
					return v1.U8 < v2.U;
				else if (v2.IsByRef)
					return v1.U8 < v2.U;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.I:
			case ElementType.ByRef:
				if (v2.IsI)
					return v1.U < v2.U;
				else if (v2.IsByRef)
					return v1.U < v2.U;
				else if (v2.IsI4)
					return v1.U < v2.U4;
				else if (v2.IsI8)
					return v1.U < v2.U8;
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R4:
				if (v2.IsR4)
					return !(v1.R4 >= v2.R4);
				else if (v2.IsR8)
					return !(v1.R4 >= v2.R8);
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			case ElementType.R8:
				if (v2.IsR8)
					return !(v1.R8 >= v2.R8);
				else if (v2.IsR4)
					return !(v1.R8 >= v2.R4);
				else
					throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			default:
				throw new InvalidProgramException("Binary comparision operation: type mismatch.");
			}
		}
		#endregion

		#region Conversion
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvertI4(ref InterpreterSlot slot, int value) {
			slot.I4 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I4 | AnnotatedElementType.Unmanaged;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvertI8(ref InterpreterSlot slot, long value) {
			slot.I8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I8 | AnnotatedElementType.Unmanaged;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvertI(ref InterpreterSlot slot, nint value) {
			slot.I = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.I | AnnotatedElementType.Unmanaged;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvertR4(ref InterpreterSlot slot, float value) {
			slot.R4 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R4 | AnnotatedElementType.Unmanaged;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ConvertR8(ref InterpreterSlot slot, double value) {
			slot.R8 = value;
			slot.AnnotatedElementType = (AnnotatedElementType)ElementType.R8 | AnnotatedElementType.Unmanaged;
		}
		#endregion

		#region Array
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint Ldelema(int elementSize, InterpreterMethodContext methodContext) {
			ref var indexSlot = ref methodContext.Pop();
			ref var arraySlot = ref methodContext.Pop();
			nint arrayPtr = arraySlot.I;
			if (arrayPtr == 0)
				throw new NullReferenceException();
			arrayPtr += sizeof(nint);
			int length = (int)*(nint*)arrayPtr;
			int index = indexSlot.I4;
			if (index < 0 || index >= length)
				throw new IndexOutOfRangeException();
			return arrayPtr + (elementSize * (index + 1));
		}
		#endregion

		#region Variable
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
					SetAnnotatedElementType(ref arguments[i], type);
				}
			}
			if (methodContext.Method.HasReturnType)
				ConvertI(ref arguments[arguments.Length - 1], stubArguments[stubArguments.Length - 1]);
			return arguments;
		}
		#endregion

		#region Dnlib
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

		#region Miscellaneous
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void LoadAny(nint source, ref InterpreterSlot destination, TypeDesc type, InterpreterMethodContext methodContext) {
			if (IsClassStackNormalized(type)) {
				destination.I = source;
				SetAnnotatedElementType(ref destination, type);
			}
			else {
				Ldobj(source, ref destination, type, methodContext);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void SetAny(ref InterpreterSlot source, nint destination, TypeDesc type) {
			if (IsClassStackNormalized(type))
				*(nint*)destination = source.I;
			else
				Stobj(ref source, destination, type);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint PushObject(object value, InterpreterMethodContext methodContext) {
			nint objectRef = PinObject(value, methodContext);
			methodContext.Push(objectRef, ElementType.Class);
			return objectRef;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static nint PushObject(object value, TypeDesc type, InterpreterMethodContext methodContext) {
			nint objectRef = PinObject(value, methodContext);
			ref var slot = ref methodContext.Push();
			slot.I = objectRef;
			SetAnnotatedElementType(ref slot, type);
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
		#endregion
	}
}
