using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Interpreter : Expr.Visitor<TypedObject>, Stmt.Visitor<object> 
    {
        public readonly Environment globals = new Environment();
        private Environment environment;

        public Interpreter()
        {
            globals.define(native("clock"), new TypedObject(EvalType.FUNCTION, new ClockNativeFunction(), null));
            globals.define(native("input"), new TypedObject(EvalType.FUNCTION, new InputNativeFunction(), null));
            globals.define(native("stringToNumber"), new TypedObject(EvalType.FUNCTION, new StringToNumberNativeFunction(), null));
            globals.define(native("toString"), new TypedObject(EvalType.FUNCTION, new ToStringNativeFunction(), null));
            globals.define(native("getType"), new TypedObject(EvalType.FUNCTION, new GetTypeNativeFunction(), null));
            environment = globals;
        }

        private Token native(string name)
        {
            return new Token(TokenType.IDENTIFIER, name, null, -1);
        }

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

        private string stringify(TypedObject o)
        {
            if (o.GetValue() == null) return "NIL";
            return o.GetValue().ToString();
        }



        // *** EXPRESSION ***
        // === Expression Visit Methods ===
        public TypedObject visitLiteralExpr(Expr.Literal expr)
        {
            return new TypedObject(expr.evalType, expr.value, expr.origin);
        }

        public TypedObject visitLogicalExpr(Expr.Logical expr)
        {
            TypedObject left = evaluate(expr.left);

            if (expr.op.type == TokenType.OR)
            {
                if (isTruthy(left)) return left;
            }
            else
            {
                if (!isTruthy(left)) return left;
            }

            return evaluate(expr.right);
        }

        public TypedObject visitGroupingExpr(Expr.Grouping expr)
        {
            return evaluate(expr.expression);
        }

        public TypedObject visitUnaryExpr(Expr.Unary expr)
        {
            TypedObject right = evaluate(expr.right);

            switch (expr.op.type)
            {
                case TokenType.BANG:
                    return new TypedObject(EvalType.BOOL, !isTruthy(right), expr.op);
                case TokenType.MINUS:
                    checkNumberOperands(expr.op, right);
                    return right;
                
            }

            return null;
        }

        public TypedObject visitBinaryExpr(Expr.Binary expr)
        {
            TypedObject left = evaluate(expr.left);
            TypedObject right = evaluate(expr.right);

            bool leftIsInt = left.varType == EvalType.INT;
            bool rightIsInt = right.varType == EvalType.INT;
            

            switch (expr.op.type)
            {
                // ---Numerical-Exclusive Comparators---
                case TokenType.GREATER:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject(EvalType.BOOL,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        > ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.GREATER_EQUAL:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject(EvalType.BOOL,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        >= ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.LESS:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject(EvalType.BOOL,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        < ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.LESS_EQUAL:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject(EvalType.BOOL,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        <= ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);

                // ---Universal Comparators---
                case TokenType.BANG_EQUAL:
                    return new TypedObject(EvalType.BOOL, !isEqual(left, right),
                        expr.op);
                case TokenType.EQUAL_EQUAL:
                    return new TypedObject(EvalType.BOOL, isEqual(left, right),
                        expr.op);

                // ---Arithmetic---
                case TokenType.MINUS:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject((leftIsInt && rightIsInt) ? EvalType.INT : EvalType.FLOAT,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        - ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.PLUS:
                    // Special: Concatenate strings
                    if (left.GetValue().varType == EvalType.STRING && right.GetValue().varType == EvalType.STRING)
                    {
                        return new TypedObject(EvalType.STRING, (string)left.GetValue().value + (string)right.GetValue().value,
                        expr.op);
                    }


                    // Normal arithmetic add
                    checkNumberOperands(expr.op, left.GetValue(), right.GetValue());
                    return new TypedObject((leftIsInt && rightIsInt) ? EvalType.INT : EvalType.FLOAT,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        + ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.SLASH:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject((leftIsInt && rightIsInt) ? EvalType.INT : EvalType.FLOAT,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        / ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.STAR:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject((leftIsInt && rightIsInt) ? EvalType.INT : EvalType.FLOAT,
                        ((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value)
                        * ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value),
                        expr.op);
                case TokenType.CARET:
                    checkNumberOperands(expr.op, left, right);
                    return new TypedObject((leftIsInt && rightIsInt) ? EvalType.INT : EvalType.FLOAT, 
                        Math.Pow(((leftIsInt) ? (int)left.GetValue().value : (double)left.GetValue().value), 
                         ((rightIsInt) ? (int)right.GetValue().value : (double)right.GetValue().value)),
                        expr.op);
            }

            return null;
        }

        public TypedObject visitCallExpr(Expr.Call expr)
        {
            TypedObject callee = evaluate(expr.callee);

            List<TypedObject> arguments = new List<TypedObject>();
            foreach (Expr argument in expr.arguments)
            {
                arguments.Add(evaluate(argument));
            }

            if (!(callee.GetValue().value is Callable)) {
                throw new RuntimeError(expr.paren, "Can only call functions and classes.");

            }

            


            Callable function = (Callable)callee.value;
            // TODO: Support overloading
            if (function.paramTypes.Count != arguments.Count)
            {
                throw new RuntimeError(expr.paren, "Invalid number of arguments: expecting " + function.paramTypes.Count.ToString() +
                    ", received " + arguments.Count.ToString() + ".");
            }
            for (int i = 0; i < function.paramTypes.Count; i++)
            {
                EvalType argType = arguments[i].GetValue().varType;
                if (!matchingEvalTypes(function.paramTypes[i], argType))
                {
                    throw new RuntimeError(expr.paren, "Argument " + (i + 1).ToString() + " type mismatch: expecting '" +
                        function.paramTypes[i].ToString() + "', received '" + arguments[i].varType.ToString() + "'.");
                }
            }

            return function.CALL(this, arguments);
        }

        public TypedObject visitVariableExpr(Expr.Variable expr)
        {
            return environment.getVar(expr.name);
        }

        public TypedObject visitAssignExpr(Expr.Assign expr)
        {
            TypedObject value = evaluate(expr.value);

            environment.assign(expr.name, value);
            return value;
        }


        // === Checks and Helper Methods ===
        private void checkNumberOperands(Token op, TypedObject operand)
        {
            if (TypedObject.matchingEvalTypes(EvalType.FLOAT, operand.varType)) return;
            throw new RuntimeError(op, "Operand must be a number; was type '" + operand.GetValue().varType + "'.");
        }

        private void checkNumberOperands(Token op, TypedObject left, TypedObject right)
        {
            if (!TypedObject.matchingEvalTypes(EvalType.FLOAT, left.GetValue().varType))
            {
                throw new RuntimeError(op, "Operands must be numbers; left was type '" + left.GetValue().varType.ToString() + "'.");
            } 
            if (!TypedObject.matchingEvalTypes(EvalType.FLOAT, right.GetValue().varType))
            {
                throw new RuntimeError(op, "Operands must be numbers; right was type '" + right.GetValue().varType.ToString() + "'.");
            }

            return;
        }

        private bool isEqual(TypedObject a, TypedObject b)
        {
            a = a.GetValue();
            b = b.GetValue();
            if (a.varType == EvalType.NIL && b.varType == EvalType.NIL) return true;
            if (a.varType == EvalType.NIL || b.varType == EvalType.NIL) return false;

            return a.value.Equals(b.value);
        }

        private bool isTruthy(TypedObject o)
        {
            if (o == null) return false;
            if (o.value is bool) return (bool)o.value;
            return true;
        }

        private TypedObject evaluate(Expr expr)
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

        public object visitFunctionStmt(Stmt.Function stmt)
        {
            // TODO: Revisit and consider how to make it a typed object
            Function function = new Function(stmt);
            environment.define(stmt.name, new TypedObject(EvalType.FUNCTION, function, stmt.name));
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
            TypedObject value = evaluate(stmt.expression);
            Console.WriteLine(stringify(value));
            return null;
        }

        public object visitPrint_ErrStmt(Stmt.Print_Err stmt)
        {
            TypedObject value = evaluate(stmt.expression);

            // Hold the current color and print error in red before restoring color
            ConsoleColor color = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Error.WriteLine(stringify(value));
            Console.ForegroundColor = color;
            return null;
        }

        public object visitReturnStmt(Stmt.Return stmt)
        {
            TypedObject value = new TypedObject(EvalType.NIL, null, stmt.keyword);
            if (stmt.value != null) value = evaluate(stmt.value);
            if (stmt.returnType == EvalType.VOID && value.value != null)
            {
                throw new RuntimeError(stmt.keyword, "Invalid return: cannot return value with function type 'VOID'");
            }
            if (!TypedObject.matchingEvalTypes(stmt.returnType, value.GetValue().varType))
            {
                throw new RuntimeError(stmt.keyword, "Invalid return type: expecting '" + stmt.returnType.ToString()
                    + "', received '" + value.GetValue().varType.ToString() + "'.");
            }
            throw new Return(value.varType, value.value);
        }

        public object visitIntStmt(Stmt.Int stmt)
        {
            TypedObject value = new TypedObject(EvalType.INT, (int)0, stmt.name);
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer).GetValue();
                if (!matchingEvalTypes(EvalType.INT, value.varType))
                {
                    throw new RuntimeError(stmt.name, "Cannot assign type '" + value.varType.ToString() +
                    "' to variable '" + stmt.name.lexeme + "' of type '" + EvalType.INT.ToString());
                }
            }

            environment.define(stmt.name, value);
            return null;
        }

        public object visitFloatStmt(Stmt.Float stmt)
        {
            TypedObject value = new TypedObject(EvalType.FLOAT, (double)0.0, stmt.name);
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer).GetValue();
                if (!matchingEvalTypes(EvalType.FLOAT, value.varType))
                {
                    throw new RuntimeError(stmt.name, "Cannot assign type '" + value.varType.ToString() +
                    "' to variable '" + stmt.name.lexeme + "' of type '" + EvalType.FLOAT.ToString());
                }
            }

            environment.define(stmt.name, value);
            return null;
        }

        public object visitBoolStmt(Stmt.Bool stmt)
        {
            TypedObject value = new TypedObject(EvalType.BOOL, (bool)false, stmt.name);
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer).GetValue();
                if (!matchingEvalTypes(EvalType.BOOL, value.varType))
                {
                    throw new RuntimeError(stmt.name, "Cannot assign type '" + value.varType.ToString() +
                    "' to variable '" + stmt.name.lexeme + "' of type '" + EvalType.BOOL.ToString());
                }
            }

            environment.define(stmt.name, value);
            return null;
        }

        public object visitStringStmt(Stmt.String stmt)
        {
            TypedObject value = new TypedObject(EvalType.STRING, (string)"", stmt.name);
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer).GetValue();
                if (!matchingEvalTypes(EvalType.STRING, value.varType))
                {
                    throw new RuntimeError(stmt.name, "Cannot assign type '" + value.varType.ToString() +
                    "' to variable '" + stmt.name.lexeme + "' of type '" + EvalType.STRING.ToString());
                }
            }

            environment.define(stmt.name, value);
            return null;
        }

        public object visitVarStmt(Stmt.Var stmt)
        {
            TypedObject value = new TypedObject(EvalType.TYPELESS, null, stmt.name);
            if (stmt.initializer != null)
            {
                value = evaluate(stmt.initializer).GetValue();
            }

            environment.define(stmt.name, value);
            return null;
        }

        public object visitWhileStmt(Stmt.While stmt)
        {
            while (isTruthy(evaluate(stmt.condition)))
            {
                execute(stmt.body);
            }
            return null;
        }

        // === Helper Methods ===
        public void executeBlock(List<Stmt> statements, Environment environment)
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

        private bool matchingEvalTypes(EvalType requiredType, EvalType compareType, bool warn = false, Token warnAtToken = null)
        {

            if (requiredType == EvalType.TYPELESS && compareType != EvalType.VOID) return true;
            if (compareType == EvalType.TYPELESS && requiredType != EvalType.VOID)
            {
                // Warn if warning is requested
                return true;
            }

            if ((requiredType == EvalType.INT || requiredType == EvalType.FLOAT) &&
                (compareType == EvalType.INT || compareType == EvalType.FLOAT))
            {
                return true;
            }

            return requiredType == compareType;
        }
    }
}

