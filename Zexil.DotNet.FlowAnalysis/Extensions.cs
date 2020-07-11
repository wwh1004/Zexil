using System;
using System.Collections.Generic;
using dnlib.DotNet;
using dnlib.DotNet.Emit;

namespace Zexil.DotNet.FlowAnalysis {
	/// <summary>
	/// Extensions
	/// </summary>
	public static class Extensions {
		/// <summary>
		/// Creates method block from methodDef.
		/// NOTICE: <see cref="CilBody.SimplifyMacros"/> will be called!
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
			body.Instructions.Clear();
			body.Instructions.AddRange(instructions);
			body.ExceptionHandlers.Clear();
			body.ExceptionHandlers.AddRange(exceptionHandlers);
			body.Variables.Clear();
			body.Variables.AddRange(locals);
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
		/// Gets block's parent of which scope is <paramref name="scope"/> and throws if null
		/// </summary>
		/// <param name="block"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static Block GetParent(this Block block, ScopeBlock scope) {
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
		/// Gets block's parent of which scope is <paramref name="scope"/>
		/// </summary>
		/// <param name="block"></param>
		/// <param name="scope"></param>
		/// <returns></returns>
		public static Block? GetParentNoThrow(this Block block, ScopeBlock scope) {
			if (block is null)
				throw new ArgumentNullException(nameof(block));
			if (scope is null)
				throw new ArgumentNullException(nameof(scope));

			var root = block;
			while (root.ScopeNoThrow != scope) {
				if (root is MethodBlock)
					return null;
				else
					root = root.Scope;
			}
			return root;
		}

		/// <summary>
		/// Adds items to a list
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list"></param>
		/// <param name="collection"></param>
		public static void AddRange<T>(this IList<T> list, IEnumerable<T> collection) {
			if (list is null)
				throw new ArgumentNullException(nameof(list));
			if (collection is null)
				throw new ArgumentNullException(nameof(collection));

			if (list is List<T> list2) {
				list2.AddRange(collection);
			}
			else {
				foreach (var instruction in collection)
					list.Add(instruction);
			}
		}
	}
}
