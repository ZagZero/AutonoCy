using System.Collections.Generic;

namespace AutonoCy
{
	public abstract class Expr {
		public interface Visitor<R> {
			R visitAssignExpr(Assign expr);
			R visitBinaryExpr(Binary expr);
			R visitCallExpr(Call expr);
			R visitGroupingExpr(Grouping expr);
			R visitLiteralExpr(Literal expr);
			R visitLogicalExpr(Logical expr);
			R visitUnaryExpr(Unary expr);
			R visitVariableExpr(Variable expr);
		}
		public class Assign : Expr {
			public Assign(Token name, Expr value, EvalType evalType) : base(evalType) {
				this.name = name;
				this.value = value;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitAssignExpr(this);
			}
			public readonly Token name;
			public readonly Expr value;
		}
		public class Binary : Expr {
			public Binary(Expr left, Token op, Expr right, EvalType evalType) : base(evalType) {
				this.left = left;
				this.op = op;
				this.right = right;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitBinaryExpr(this);
			}
			public readonly Expr left;
			public readonly Token op;
			public readonly Expr right;
		}
		public class Call : Expr {
			public Call(Expr callee, Token paren, List<Expr> arguments, EvalType evalType) : base(evalType) {
				this.callee = callee;
				this.paren = paren;
				this.arguments = arguments;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitCallExpr(this);
			}
			public readonly Expr callee;
			public readonly Token paren;
			public readonly List<Expr> arguments;
		}
		public class Grouping : Expr {
			public Grouping(Expr expression, EvalType evalType) : base(evalType) {
				this.expression = expression;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitGroupingExpr(this);
			}
			public readonly Expr expression;
		}
		public class Literal : Expr {
			public Literal(object value, EvalType evalType, Token origin) : base(evalType) {
				this.value = value;
                this.origin = origin;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitLiteralExpr(this);
			}
			public readonly object value;
            public readonly Token origin;
		}
		public class Logical : Expr {
			public Logical(Expr left, Token op, Expr right, EvalType evalType) : base(evalType) {
				this.left = left;
				this.op = op;
				this.right = right;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitLogicalExpr(this);
			}
			public readonly Expr left;
			public readonly Token op;
			public readonly Expr right;
		}
		public class Unary : Expr {
			public Unary(Token op, Expr right, EvalType evalType) : base(evalType) {
				this.op = op;
				this.right = right;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitUnaryExpr(this);
			}
			public readonly Token op;
			public readonly Expr right;
		}
		public class Variable : Expr {
			public Variable(Token name, EvalType evalType) : base(evalType) {
				this.name = name;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitVariableExpr(this);
			}
			public readonly Token name;
		}

		public abstract R accept<R>(Visitor<R> visitor);

        public readonly EvalType evalType;
        public Expr(EvalType evalType)
        {
            this.evalType = evalType;
        }
	}
}
