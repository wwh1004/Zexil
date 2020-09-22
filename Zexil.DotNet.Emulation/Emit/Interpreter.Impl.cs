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
						methodContext.PushI(v1.I4 + (byte*)v2.I);
					else if (v2.IsByRef)
						methodContext.PushByRef(v1.I4 + (byte*)v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 + v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 + v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 + (long)v2.I);
					else if (v2.IsByRef)
						methodContext.PushByRef(v1.I8 + (byte*)v2.I);
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I8));
					else if (v2.IsI4)
						methodContext.PushI((byte*)v1.I + v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8((long)v1.I + v2.I8);
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsI4)
						methodContext.PushByRef((byte*)v1.I + v2.I4);
					else if (v2.IsI8)
						methodContext.PushByRef((byte*)v1.I + v2.I8);
					else if (v2.IsI)
						methodContext.PushByRef((void*)(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I8));
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
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I4 + v2.I8));
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
						methodContext.PushI8(checked(sizeof(void*) == 4 ? v1.I8 + v2.I4 : v1.I8 + v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for long and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I8));
					else if (v2.IsI4)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(sizeof(void*) == 4 ? v1.I4 + v2.I8 : v1.I8 + v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for native int and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsI4)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I4));
					else if (v2.IsI8)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I8 : v1.I8 + v2.I8));
					else if (v2.IsI)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.I4 + v2.I4 : v1.I8 + v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for byref and byref: may only subtract managed pointer values.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (byref and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Add_Ovf_Un: {
				ref var v2 = ref methodContext.Pop();
				ref var v1 = ref methodContext.Pop();
				switch (v1.ElementType) {
				case ElementType.U4:
					if (v2.IsI4)
						methodContext.PushI4((int)checked(v1.U4 + v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U4 + v2.U8));
					else if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U4 + v2.U8));
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U4 + v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.U8:
					if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U8 + v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)checked(v1.U8 + v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)checked(sizeof(void*) == 4 ? v1.U8 + v2.U4 : v1.U8 + v2.U8));
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U8 + v2.U4 : v1.U8 + v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U8 + v2.U8));
					else if (v2.IsI4)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U8 + v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(sizeof(void*) == 4 ? v1.U4 + v2.U8 : v1.U8 + v2.U8));
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U8 + v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsI4)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U8 + v2.U4));
					else if (v2.IsI8)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U8 : v1.U8 + v2.U8));
					else if (v2.IsI)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 + v2.U4 : v1.U8 + v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (byref and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
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
				break;

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
						methodContext.PushI((void*)((byte*)v1.I4 - (byte*)v2.I));
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)((byte*)v1.I4 - (byte*)v2.I));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (int and ?)");
					break;
				case ElementType.I8:
					if (v2.IsI8)
						methodContext.PushI8(v1.I8 - v2.I8);
					else if (v2.IsI4)
						methodContext.PushI8(v1.I8 - v2.I4);
					else if (v2.IsI)
						methodContext.PushI8(v1.I8 - (long)v2.I);
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)((byte*)v1.I8 - (byte*)v2.I));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I8 - v2.I8));
					else if (v2.IsI4)
						methodContext.PushI((byte*)v1.I - v2.I4);
					else if (v2.IsI8)
						methodContext.PushI8((long)v1.I - v2.I8);
					else if (v2.IsByRef)
						methodContext.PushByRef((void*)(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I8 - v2.I8));
					else
						throw new InvalidProgramException("Binary arithmetic operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsByRef)
						methodContext.PushI((void*)((byte*)v1.I - (byte*)v2.I));
					else if (v2.IsI4)
						methodContext.PushByRef((byte*)v1.I - v2.I4);
					else if (v2.IsI8)
						methodContext.PushByRef((byte*)v1.I - v2.I8);
					else if (v2.IsI)
						methodContext.PushByRef((void*)(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I8 - v2.I8));
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
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I4 - v2.I8));
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
						methodContext.PushI8(checked(sizeof(void*) == 4 ? v1.I8 - v2.I4 : v1.I8 - v2.I8));
					else if (v2.IsByRef)
						throw new InvalidProgramException("Illegal arithmetic overflow operation for long and byref.");
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I8 - v2.I8));
					else if (v2.IsI4)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.I4 - v2.I4 : v1.I8 - v2.I4));
					else if (v2.IsI8)
						methodContext.PushI8(checked(sizeof(void*) == 4 ? v1.I4 - v2.I8 : v1.I8 - v2.I8));
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
				case ElementType.U4:
					if (v2.IsI4)
						methodContext.PushI4((int)checked(v1.U4 - v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U4 - v2.U8));
					else if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U4 : v1.U4 - v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (int and ?)");
					break;
				case ElementType.U8:
					if (v2.IsI8)
						methodContext.PushI8((long)checked(v1.U8 - v2.U8));
					else if (v2.IsI4)
						methodContext.PushI8((long)checked(v1.U8 - v2.U4));
					else if (v2.IsI)
						methodContext.PushI8((long)checked(sizeof(void*) == 4 ? v1.U8 - v2.U4 : v1.U8 - v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (long and ?)");
					break;
				case ElementType.I:
					if (v2.IsI)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U4 : v1.U8 - v2.U8));
					else if (v2.IsI4)
						methodContext.PushI((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U4 : v1.U8 - v2.U4));
					else if (v2.IsI8)
						methodContext.PushI8((long)checked(sizeof(void*) == 4 ? v1.U4 - v2.U8 : v1.U8 - v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (native int and ?)");
					break;
				case ElementType.ByRef:
					if (v2.IsByRef)
						methodContext.PushI((void*)checked((byte*)v1.I - (byte*)v2.I));
					else if (v2.IsI4)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U4 : v1.U8 - v2.U4));
					else if (v2.IsI8)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U8 : v1.U8 - v2.U8));
					else if (v2.IsI)
						methodContext.PushByRef((void*)checked(sizeof(void*) == 4 ? v1.U4 - v2.U4 : v1.U8 - v2.U8));
					else
						throw new InvalidProgramException("Binary arithmetic overflow operation type mismatch (byref and ?)");
					break;
				default:
					throw new InvalidProgramException("Can't do binary arithmetic on object references or non-stack-normal type on stack.");
				}
				break;
			}
			case Code.Xor:
				break;

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
