using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Interpreter : Expr.Visitor<object>, Stmt.Visitor<object> 
    {
        private Environment environment = new Environment();

        // === Entry and Evaluation Control Methods ===
        public void interpret(List<Stmt> statements)
        {
            try
            {
                foreach (Stmt statement in statements)
                {
                    execute(statement);
                }
            }
            catch (RuntimeError e)
            {
                AutonoCy_Main.runtimeError(e);
            }

        }

        private object execute(Stmt stmt)
        {
            stmt.accept(this);
            return null;
        }

        private string stringify(object o)
        {
            if (o == null) return "nil";
            return o.ToString();
        }



        // *** EXPRESSION ***
        // === Expression Visit Methods ===
        public object visitLiteralExpr(Expr.Literal expr)
        {
            return expr.value;
        }

        public object visitLogicalExpr(Expr.Logical expr)
        {
            object left = evaluate(expr.left);

            if (expr.op.type == TokenTypes.OR)
            {
                if (isTruthy(left)) return left;
            }
            else
            {
                if (!isTruthy(left)) return left;
            }

            return evaluate(expr.right);
        }

        public object visitGroupingExpr(Expr.Grouping expr)
        {
            return evaluate(expr.expression);
        }

        public object visitUnaryExpr(Expr.Unary expr)
        {
            object right = evaluate(expr.right);

            switch (expr.op.type)
            {
                case TokenTypes.BANG:
                    return !isTruthy(right);
                case TokenTypes.MINUS:
                    checkNumberOperands(expr.op, right);
                    Type t = right.GetType();
                    return (t == typeof(int))?-(int)right:-(double)right;
                
            }

            return null;
        }

        public object visitBinaryExpr(Expr.Binary expr)
        {
            object left = evaluate(expr.left);
            object right = evaluate(expr.right);

            Type leftType = left.GetType();
            Type rightType = right.GetType();

            bool leftIsInt = leftType == typeof(int);
            bool rightIsInt = rightType == typeof(int);
            

            switch (expr.op.type)
            {
                // ---Numerical-Exclusive Comparators---
                case TokenTypes.GREATER:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        > ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.GREATER_EQUAL:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        >= ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.LESS:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        < ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.LESS_EQUAL:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        <= ((rightIsInt) ? (int)right : (double)right);

                // ---Universal Comparators---
                case TokenTypes.BANG_EQUAL:
                    return !isEqual(left, right);
                case TokenTypes.EQUAL_EQUAL:
                    return isEqual(left, right);

                // ---Arithmetic---
                case TokenTypes.MINUS:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt)?(int)left:(double)left)
                        - ((rightIsInt)?(int)right:(double)right);
                case TokenTypes.PLUS:
                    // Special: Concatenate strings
                    if (left.GetType() == typeof(string) && right.GetType() == typeof(string))
                    {
                        return (string)left + (string)right;
                    }

                    // Normal arithmetic add
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        + ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.SLASH:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        / ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.STAR:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return ((leftIsInt) ? (int)left : (double)left)
                        * ((rightIsInt) ? (int)right : (double)right);
                case TokenTypes.CARET:
                    checkNumberOperands(expr.op, leftType, rightType);
                    return Math.Pow(((leftIsInt) ? (int)left : (double)left), 
                         ((rightIsInt) ? (int)right : (double)right));
            }

            return null;
        }

        public object visitCallExpr(Expr.Call expr)
        {
            object callee = evaluate(expr.callee);

            List<object> arguments = new List<object>();
            foreach (Expr argument in expr.arguments)
            {
                arguments.Add(evaluate(argument));
            }

            if (!(callee.GetType() == typeof(Callable))) {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");

            }


            Callable function = (Callable)callee;
            return function.call(this, arguments);
        }

        public object visitVariableExpr(Expr.Variable expr)
        {
            return environment.getVar(expr.name);
        }

        public object visitAssignExpr(Expr.Assign expr)
        {
            object value = evaluate(expr.value);

            environment.assign(expr.name, value);
            return value;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            while (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.body);
            }
            return null;
        }


        // === Checks and Helper Methods ===
        private void checkNumberOperands(Token op, object operand)
        {
            if (operand.GetType() == typeof(double) || operand.GetType() == typeof(int)) return;
            throw new RuntimeError(op, "Operand must be a number");
        }

        private void checkNumberOperands(Token op, Type left, Type right)
        {
            if ((left == typeof(double) || left == typeof(int))
                && (right == typeof(double) || right == typeof(int))) return;
            throw new RuntimeError(op, "Operands must be numbers.");
        }

        private bool isEqual(object a, object b)
        {
            if (a == null && b == null) return true;
            if (a == null) return false;

            return a.Equals(b);
        }

        private bool isTruthy(object o)
        {
            if (o == null) return false;
            if (o.GetType() == typeof(bool)) return (bool)o;
            return true;
        }

        private object evaluate(Expr expr)
        {
            return expr.accept(this);
        }
        // *** END EXPRESSION ***

        // *** STATEMENT ***
        // === Statement Visit Methods ===
        public object visitBlockStmt(Stmt.Block stmt)
        {
            executeBlock(stmt.statements, new Environment(environment));
            return null;
        }

        public object visitExpressionStmt(Stmt.Expression stmt)
        {
            evaluate(stmt.expression);
            return null;
        }

        public object visitIfStmt(Stmt.If stmt)
        {
            if (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.thenBranch);
            }
            else if (stmt.elseBranch != null)
            {
                execute(stmt.elseBranch);
            }
            return null;
        }

        public object visitPrintStmt(Stmt.Print stmt)
        {
            object value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
        }

        public object visitPrint_ErrStmt(Stmt.Print_Err stmt)
        {
            object value = evaluate(stmt.expression);

            // Hold the current color and print error in red before restoring color
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(stringify(value));
            Console.ForegroundColor = color;
            return null;
        }

        public object visitIntStmt(Stmt.Int stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        public object visitFloatStmt(Stmt.Float stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        public object visitBoolStmt(Stmt.Bool stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        public object visitStringStmt(Stmt.String stmt)
        {
            object value = null;
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer);
            }

            environment.define(stmt.name.lexeme, value);
            return null;
        }

        // === Helper Methods ===
        void executeBlock(List<Stmt> statements, Environment environment)
        {
            Environment previous = this.environment;
            try
            {
                this.environment = environment;
                foreach (Stmt statement in statements)
                {
                    execute(statement);
                }
            }
            finally
            {
                this.environment = previous;
            }
        }
        // *** END STATEMENT ***
    }
}
