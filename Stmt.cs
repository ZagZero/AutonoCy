using System.Collections.Generic;

namespace AutonoCy
{
	public abstract class Stmt {
		public interface Visitor<R> {
			R visitBlockStmt(Block stmt);
			R visitExpressionStmt(Expression stmt);
			R visitFunctionStmt(Function stmt);
			R visitIfStmt(If stmt);
			R visitPrintStmt(Print stmt);
			R visitPrint_ErrStmt(Print_Err stmt);
			R visitIntStmt(Int stmt);
			R visitFloatStmt(Float stmt);
			R visitStringStmt(String stmt);
			R visitBoolStmt(Bool stmt);
			R visitReturnStmt(Return stmt);
			R visitVarStmt(Var stmt);
			R visitWhileStmt(While stmt);
		}
		public class Block : Stmt {
			public Block(List<Stmt> statements) {
				this.statements = statements;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitBlockStmt(this);
			}
			public readonly List<Stmt> statements;
		}
		public class Expression : Stmt {
			public Expression(Expr expression) {
				this.expression = expression;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitExpressionStmt(this);
			}
			public readonly Expr expression;
		}
		public class Function : Stmt {
			public Function(Token name, List<Token> parameters, List<Stmt> body) {
				this.name = name;
				this.parameters = parameters;
				this.body = body;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitFunctionStmt(this);
			}
			public readonly Token name;
			public readonly List<Token> parameters;
			public readonly List<Stmt> body;
		}
		public class If : Stmt {
			public If(Expr condition, Stmt thenBranch, Stmt elseBranch) {
				this.condition = condition;
				this.thenBranch = thenBranch;
				this.elseBranch = elseBranch;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitIfStmt(this);
			}
			public readonly Expr condition;
			public readonly Stmt thenBranch;
			public readonly Stmt elseBranch;
		}
		public class Print : Stmt {
			public Print(Expr expression) {
				this.expression = expression;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitPrintStmt(this);
			}
			public readonly Expr expression;
		}
		public class Print_Err : Stmt {
			public Print_Err(Expr expression) {
				this.expression = expression;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitPrint_ErrStmt(this);
			}
			public readonly Expr expression;
		}
		public class Int : Stmt {
			public Int(Token name, Expr initializer) {
				this.name = name;
				this.initializer = initializer;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitIntStmt(this);
			}
			public readonly Token name;
			public readonly Expr initializer;
		}
		public class Float : Stmt {
			public Float(Token name, Expr initializer) {
				this.name = name;
				this.initializer = initializer;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitFloatStmt(this);
			}
			public readonly Token name;
			public readonly Expr initializer;
		}
		public class String : Stmt {
			public String(Token name, Expr initializer) {
				this.name = name;
				this.initializer = initializer;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitStringStmt(this);
			}
			public readonly Token name;
			public readonly Expr initializer;
		}
		public class Bool : Stmt {
			public Bool(Token name, Expr initializer) {
				this.name = name;
				this.initializer = initializer;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitBoolStmt(this);
			}
			public readonly Token name;
			public readonly Expr initializer;
		}
		public class Return : Stmt {
			public Return(Token keyword, Expr value) {
				this.keyword = keyword;
				this.value = value;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitReturnStmt(this);
			}
			public readonly Token keyword;
			public readonly Expr value;
		}
		public class Var : Stmt {
			public Var(Token name, Expr initializer) {
				this.name = name;
				this.initializer = initializer;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitVarStmt(this);
			}
			public readonly Token name;
			public readonly Expr initializer;
		}
		public class While : Stmt {
			public While(Expr condition, Stmt body) {
				this.condition = condition;
				this.body = body;
			}


			public override R accept<R>(Visitor<R> visitor) {
				return visitor.visitWhileStmt(this);
			}
			public readonly Expr condition;
			public readonly Stmt body;
		}

		public abstract R accept<R>(Visitor<R> visitor);
	}
}
