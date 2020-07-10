using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.ControlFlow {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Creates method block from methodDef.
		/// NOTICE: <see cref="CilBody.SimplifyMacros"/> will be called!!!
		/// </summary>
		/// <param name="methodDef"></param>
		/// <returns></returns>
		public static MethodBlock ToMethodBlock(this MethodDef methodDef) {
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));

			methodDef.Body.SimplifyMacros(methodDef.Parameters);
			return CodeParser.Parse(methodDef.Body.Instructions, methodDef.Body.ExceptionHandlers);
		}

		/// <summary>
		/// Restores <see cref="MethodDef"/> from <see cref="MethodBlock"/>
		/// </summary>
		/// <param name="methodDef"></param>
		/// <param name="methodBlock"></param>
		public static void FromMethodBlock(this MethodDef methodDef, MethodBlock methodBlock) {
			if (methodDef is null)
				throw new ArgumentNullException(nameof(methodDef));
			if (methodBlock is null)
				throw new ArgumentNullException(nameof(methodBlock));

			var body = methodDef.Body;
			if (body is null)
				throw new InvalidOperationException();
			CodeGenerator.Generate(methodBlock, out var instructions, out var exceptionHandlers, out var locals);
			ReplaceList(instructions, body.Instructions);
			ReplaceList(exceptionHandlers, body.ExceptionHandlers);
			ReplaceList(locals, body.Variables);

			static void ReplaceList<T>(IList<T> source, IList<T> destination) {
				destination.Clear();
				if (destination is List<T> destination2) {
					destination2.AddRange(source);
				}
				else {
					foreach (var item in source)
						destination.Add(item);
				}
			}
		}

		/// <summary>
		/// Gets the first basic block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static BasicBlock First(this Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			return (BasicBlock)Impl(block);

			static Block Impl(Block b) {
				if (b is BasicBlock)
					return b;
				else
					return Impl(((ScopeBlock)b).FirstBlock);
			}
		}

		/// <summary>
		/// Gets the last basic block
		/// </summary>
		/// <param name="block"></param>
		/// <returns></returns>
		public static BasicBlock Last(this Block block) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));

			return (BasicBlock)Impl(block);

			static Block Impl(Block b) {
				if (b is BasicBlock)
					return b;
				else
					return Impl(((ScopeBlock)b).LastBlock);
			}
		}

		/// <summary>
		/// Gets block's root of which scope is <paramref name="scope"/>
		/// </summary>
		/// <param name="block"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static Block GetRoot(this Block block, ScopeBlock scope) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));
			if (scope is null)
				throw new ArgumentNullException(nameof(scope));

			var root = block;
			while (root.Scope != scope)
				root = root.Scope;
			return root;
		}

		/// <summary>
		/// Adds instructions to a list
		/// </summary>
		/// <param name="instructions"></param>
		/// <param name="collection"></param>
		public static void AddRange(this IList<Instruction> instructions, IEnumerable<Instruction> collection) {
			if (instructions is null)
				throw new ArgumentNullException(nameof(instructions));
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			if (instructions is List<Instruction> list) {
				list.AddRange(collection);
			}
			else {
				foreach (var instruction in collection)
					instructions.Add(instruction);
			}
		}
	}
}
