namespace Zexil.DotNet.Ast {
	public abstract class AstNode {
		protected OpCode _opCode;

		public OpCode OpCode {
			get => _opCode;
			set => _opCode = value;
		}

		internal AstNode() {
			_opCode = null;
			// TODO
		}

		internal AstNode(OpCode opCode) {
			_opCode = opCode;
		}
	}
}
