using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Parser
    {
        private List<Token> tokens;
        private int current = 0;
        private EvalType returnType = EvalType.VOID;

        Environment environment;
        public readonly Environment globals = new Environment();

        public Parser()
        {

            // Include globals to make sure parse step doesn't catch them as being missing
            globals.define(native("clock("), storeFun(EvalType.FLOAT, new List<Parameter> { }, null), true, this);
            globals.define(native("input("), storeFun(EvalType.STRING, new List<Parameter> { }, null), true, this);
            globals.define(native("stringToNumber("), storeFun(EvalType.FLOAT, 
                new List<Parameter> { new Parameter(EvalType.STRING, new Token(TokenType.NIL, "", "", -1)) }, null), true, this);
            globals.define(native("toString("), storeFun(EvalType.STRING, 
                new List<Parameter> { new Parameter(EvalType.TYPELESS, new Token(TokenType.NIL, "", "", -1)) }, null), true, this);
            globals.define(native("getType("), storeFun(EvalType.STRING,
                new List<Parameter> { new Parameter(EvalType.STRING, new Token(TokenType.NIL, "", "", -1)) }, null), true, this);
            environment = new Environment(globals);
        }

        private Token native(string name)
        {
            return new Token(TokenType.IDENTIFIER, name, null, -1);
        }

        private TypedObject storeVar(EvalType type, Token token)
        {
            return new TypedObject(type, null, token, true);
        }

        private TypedObject storeFun(EvalType type, List<Parameter> parameters, Token token)
        {
            if (token == null)
            {
                token = new Token(TokenType.FUN, "Globals", null, -1);
            }
            return new TypedObject(type, parameters, token, true);
        }

        public List<Stmt> parse(List<Token> tokens)
        {
            this.tokens = tokens;
            List<Stmt> statements = new List<Stmt>();
            current = 0;
            while (!isAtEnd())
            {
                statements.Add(declaration());
            }

            return statements;
        }

        private Stmt declaration()
        {
            try
            {
                if (match(TokenType.FUN, TokenType.VOID)) return function("function");
                if (match(TokenType.INT, TokenType.FLOAT, TokenType.STR, TokenType.BOOL, TokenType.VAR))
                    return varDeclaration();
                return statement();
            }
            catch (ParseError e)
            {
                syncronize();
                return null;
            }
        }

        private Stmt.Function function(string kind)
        {
            // Get the token that triggered this method (should be FUN or VOID)
            Token typeHold = previous();
            // Check if the next token provides a type, given that the triggering one isn't VOID 
            if (typeHold.type != TokenType.VOID && match(TokenType.INT, TokenType.FLOAT, 
                TokenType.BOOL, TokenType.STR, TokenType.VOID))
            {
                // Set the typeHold token to the matched token
                typeHold = previous();
            }
            // Look for the identifier
            Token name = consume(TokenType.IDENTIFIER, "Expect " + kind + " name.");
            // Look for the opening parenthesis
            consume(TokenType.LEFT_PAREN, "Expect '(' after " + kind + " name.");

            // Finish up in the shared part
            return finishFunction(name, kind, typeHold);
        }

        private Stmt.Function finishFunction (Token name, string kind, Token typeHold)
        {
            // Initialize a list to hold the parameters
            List<Parameter> parameters = new List<Parameter>();

            // Check to make sure the next token isn't the closing parenthesis
            if (!check(TokenType.RIGHT_PAREN))
            {
                // Loop for every following comma.er
                do
                {
                    // Hold on to the comma or parenthesis token for error-reporting purposes
                    Token last = previous();
                    // Arbitrarily set the max parameters to 16
                    if (parameters.Count() >= 16)
                    {
                        error(peek(), "Cannot have more than 16 parameters.");
                    }
                    // Check for types that indicate a declaration
                    if (match(TokenType.INT, TokenType.FLOAT, TokenType.STR, TokenType.BOOL, TokenType.VAR))
                    {
                        // Get the type of the parameter
                        Token varType = previous();
                        // Look for an identifier
                        Token paramName = consume(TokenType.IDENTIFIER, "Expected parameter name after '" + varType.lexeme + "'.");
                        // Add to the parameters list (each needs a name token and a type (EvalType))
                        parameters.Add(new Parameter(tokenToEvalType(varType.type), paramName));
                    }
                    // Didn't have a declaration after a comma
                    else { throw error(last, "Expect parameter declaration after '" + last.lexeme + "'."); }
                } while (match(TokenType.COMMA));
            }
            // Check for closing parenthesis
            consume(TokenType.RIGHT_PAREN, "Expect ')' after parameters.");

            // Look for opening brace
            consume(TokenType.LEFT_BRACE, "Expect '{' before " + kind + " body.");
            environment.define(new Token(name.type, name.lexeme + "(", name.literal, name.line), 
                storeFun(tokenToEvalType(typeHold.type), parameters, name), true, this);
            EvalType previousReturn = returnType;
            returnType = tokenToEvalType(typeHold.type);
            List<Stmt> body = block(parameters);
            returnType = previousReturn;
            return new Stmt.Function(name, parameters, body, tokenToEvalType(typeHold.type));
        }

        private Stmt varDeclaration()
        {
            // Hold on to the declaring token for type reasons
            Token typeHold = previous();
            // Check for an identifier
            Token name = consume(TokenType.IDENTIFIER, "Expect identifier name after '" + typeHold.lexeme + "'.");

            // Check for an initializer
            Expr initializer = null;
            if (match(TokenType.EQUAL))
            {
                initializer = expression();
                if (!matchingEvalTypes(tokenToEvalType(typeHold.type), initializer.evalType))
                {
                    error(previous(), "Cannot assign type '" + initializer.evalType.ToString() +
                        "' to variable '" + name.lexeme + "' of type '" + tokenToEvalType(typeHold.type).ToString() + "'.");
                }
            }

            if (match(TokenType.LEFT_PAREN) && typeHold.type != TokenType.VAR)
            {
                // Actually a function (maybe method...)
                return finishFunction(name, "function", typeHold);
            }


            consume(TokenType.SEMICOLON, "Expect ';' after variable declaration");

            switch (typeHold.type)
            {
                case TokenType.BOOL:
                    environment.define(name, storeVar(EvalType.BOOL, name), true, this);
                    return new Stmt.Bool(name, initializer);
                case TokenType.FLOAT:
                    environment.define(name, storeVar(EvalType.FLOAT, name), true, this);
                    return new Stmt.Float(name, initializer);
                case TokenType.INT:
                    environment.define(name, storeVar(EvalType.INT, name), true, this);
                    return new Stmt.Int(name, initializer);
                case TokenType.STR:
                    environment.define(name, storeVar(EvalType.STRING, name), true, this);
                    return new Stmt.String(name, initializer);
                case TokenType.VAR:
                    environment.define(name, storeVar(EvalType.TYPELESS, name), true, this);
                    return new Stmt.Var(name, initializer);
                default:
                    throw error(typeHold, "Unhandled type, interpreter shouldn't let this happen.");
            }
        }

        private Stmt statement()
        {
            if (match(TokenType.FOR)) return forStatement();
            if (match(TokenType.IF)) return ifStatement();
            if (match(TokenType.PRINT)) return printStatement(false);
            if (match(TokenType.PRINT_ERR)) return printStatement(true);
            if (match(TokenType.RETURN)) return returnStatement();
            if (match(TokenType.WHILE)) return whileStatement();
            if (match(TokenType.LEFT_BRACE)) return new Stmt.Block(block());

            return expressionStatement();
        }

        private Stmt forStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (match(TokenType.SEMICOLON))
            {
                initializer = null;
            }
            else if (match(TokenType.INT, TokenType.FLOAT, TokenType.VAR))
            {
                initializer = varDeclaration();
            }
            else
            {
                initializer = expressionStatement();
            }

            Expr condition = null;
            if (!check(TokenType.SEMICOLON))
            {
                condition = expression();
            }
            consume(TokenType.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!check(TokenType.RIGHT_PAREN))
            {
                increment = expression();
            }
            consume(TokenType.RIGHT_PAREN, "Expect ')' after for clauses.");

            Stmt body = statement();

            if (increment != null)
            {
                body = new Stmt.Block(new List<Stmt> { body, new Stmt.Expression(increment)});
            }

            if (condition == null) condition = new Expr.Literal(true, EvalType.BOOL, previous());
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt> { initializer, body });
            }

            return body;
        }

        private Stmt ifStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'if'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = statement();
            Stmt elseBranch = null;
            if (match(TokenType.ELSE))
            {
                elseBranch = statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Stmt whileStatement()
        {
            consume(TokenType.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = expression();
            consume(TokenType.RIGHT_PAREN, "Expect ')' after while condition.");
            Stmt body = statement();

            return new Stmt.While(condition, body);
        }

        private Stmt printStatement(bool err)
        {
            Expr value = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after value.");
            if (err) return new Stmt.Print_Err(value);
            return new Stmt.Print(value);
        }

        private Stmt expressionStatement()
        {
            Expr value = expression();
            consume(TokenType.SEMICOLON, "Expect ';' after value.");
            return new Stmt.Expression(value);
        }

        private Stmt returnStatement()
        {
            Token keyword = previous();
            Expr value = null;
            if (!check(TokenType.SEMICOLON))
            {
                if (returnType == EvalType.VOID)
                {
                    throw error(keyword, "Invalid return: cannot return value with function type 'VOID'.");
                }
                value = expression();
                if (!matchingEvalTypes(returnType, value.evalType))
                {
                    error(keyword, "Invalid return type: expecting '" + returnType.ToString() + "', received '" + value.evalType + "'.");
                }
            }
            else
            {
                if (returnType != EvalType.VOID && returnType != EvalType.TYPELESS)
                {
                    error(keyword, "Invalid return: must return a value of type '" + returnType.ToString() + "'.");
                }
            }

            consume(TokenType.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value, returnType);
        }

        private List<Stmt> block(List<Parameter> addToScope = null)
        {
            List<Stmt> statements = new List<Stmt>();

            environment = new Environment(environment);
            if (addToScope != null)
            {
                foreach (Parameter p in addToScope)
                {
                    environment.define(p.name, storeVar(p.varType, null), true, this);
                }
            }

            while (!check(TokenType.RIGHT_BRACE) && !isAtEnd())
            {
                statements.Add(declaration());
            }

            consume(TokenType.RIGHT_BRACE, "Expect '}' after block.");

            environment = environment.enclosing;

            return statements;
        }

        private Expr assignment()
        {
            Expr expr = or();

            if (match(TokenType.EQUAL))
            {
                Token equals = previous();
                Expr value = assignment();

                if (expr is Expr.Variable)
                {
                    Token name = ((Expr.Variable)expr).name;
                    if (environment.getVar(name, true) == null)
                    {
                        error(name, "Variable '" + name.lexeme + "' not defined.");
                    }
                    TypedObject type = (TypedObject)environment.getVar(name, true);
                    if (!matchingEvalTypes(type.varType, value.evalType))
                    {
                        error(equals, "Cannot assign type '" + value.evalType.ToString() +
                        "' to variable '" + name.lexeme + "' of type '" + type.varType.ToString() + "'.");
                    }
                    return new Expr.Assign(name, value, value.evalType);
                }

                error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr or()
        {
            Expr expr = and();

            while (match(TokenType.OR))
            {
                Token op = previous();
                Expr right = and();
                if (!matchingEvalTypes(EvalType.BOOL, expr.evalType))
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }
                if (!matchingEvalTypes(EvalType.BOOL, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }

                expr = new Expr.Logical(expr, op, right, EvalType.BOOL);
            }

            return expr;
        }

        private Expr and()
        {
            Expr expr = equality();

            while (match(TokenType.AND))
            {
                Token op = previous();
                Expr right = equality();
                if (!matchingEvalTypes(EvalType.BOOL, expr.evalType))
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }
                if (!matchingEvalTypes(EvalType.BOOL, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }

                expr = new Expr.Logical(expr, op, right, EvalType.BOOL);
            }

            return expr;
        }

        private Expr expression()
        {
            return assignment();
        }

        private Expr equality()
        {
            Expr expr = comparison();

            while (match(TokenType.BANG_EQUAL, TokenType.EQUAL_EQUAL))
            {
                Token op = previous();
                Expr right = comparison();
                if (expr.evalType == EvalType.VOID)
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }
                if (right.evalType == EvalType.VOID)
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'BOOL'.");
                }
                expr = new Expr.Binary(expr, op, right, EvalType.BOOL);
            }

            return expr;
        }

        private Expr comparison()
        {
            Expr expr = addition();

            while (match(TokenType.GREATER, TokenType.GREATER_EQUAL, TokenType.LESS, TokenType.LESS_EQUAL))
            {
                Token op = previous();
                Expr right = addition();
                if (!matchingEvalTypes(EvalType.FLOAT, expr.evalType))
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }
                if (!matchingEvalTypes(EvalType.FLOAT, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }
                
                expr = new Expr.Binary(expr, op, right, EvalType.BOOL);
            }

            return expr;
        }

        private Expr addition()
        {
            Expr expr = multiplication();

            while (match(TokenType.MINUS, TokenType.PLUS))
            {
                bool exprHandled = false;
                Token op = previous();
                Expr right = multiplication();
                if (op.type == TokenType.PLUS)
                {
                    if (right.evalType == EvalType.TYPELESS && expr.evalType == EvalType.TYPELESS)
                    {
                        expr = new Expr.Binary(expr, op, right, EvalType.TYPELESS);
                        exprHandled = true;
                    }
                    if (expr.evalType == EvalType.STRING || right.evalType == EvalType.STRING)
                    {
                        if (matchingEvalTypes(EvalType.STRING, expr.evalType) && matchingEvalTypes(EvalType.STRING, right.evalType))
                        {
                            expr = new Expr.Binary(expr, op, right, EvalType.STRING);
                        }
                        else
                        {
                            error(op, "Unexpected type '" + expr.evalType.ToString()
                                + "' and '" + right.evalType.ToString() + "' for string concatenation '" 
                                + op.lexeme + "'; both must be type 'STRING'.");
                        }
                        exprHandled = true;
                    }

                }
                if (!exprHandled)
                {
                    if (!matchingEvalTypes(EvalType.FLOAT, expr.evalType))
                    {
                        error(op, "Unexpected type '" + expr.evalType.ToString()
                            + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                    }
                    if (!matchingEvalTypes(EvalType.FLOAT, right.evalType))
                    {
                        error(op, "Unexpected type '" + right.evalType.ToString()
                            + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                    }

                    EvalType eval = EvalType.FLOAT;
                    if (expr.evalType == EvalType.INT && right.evalType == EvalType.INT)
                    {
                        eval = EvalType.INT;
                    }
                    expr = new Expr.Binary(expr, op, right, eval);

                }
            }

            return expr;
        }

        private Expr multiplication()
        {
            Expr expr = exponent();

            while (match(TokenType.SLASH, TokenType.STAR))
            {
                Token op = previous();
                Expr right = exponent();
                if (!matchingEvalTypes(EvalType.FLOAT, expr.evalType))
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }
                if (!matchingEvalTypes(EvalType.FLOAT, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }

                EvalType eval = EvalType.FLOAT;
                if (expr.evalType == EvalType.INT && right.evalType == EvalType.INT)
                {
                    eval = EvalType.INT;
                }
                expr = new Expr.Binary(expr, op, right, eval);
            }

            return expr;
        }

        private Expr exponent()
        {
            Expr expr = unary();

            while (match(TokenType.CARET))
            {
                Token op = previous();
                Expr right = unary();
                if (!matchingEvalTypes(EvalType.FLOAT, expr.evalType))
                {
                    error(op, "Unexpected type '" + expr.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }
                if (!matchingEvalTypes(EvalType.FLOAT, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for binary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }

                EvalType eval = EvalType.FLOAT;
                if (expr.evalType == EvalType.INT && right.evalType == EvalType.INT)
                {
                    eval = EvalType.INT;
                } 
                expr = new Expr.Binary(expr, op, right, eval);
            }

            return expr;
        }

        private Expr unary()
        {
            if (match(TokenType.BANG))
            {
                Token op = previous();
                Expr right = unary();
                if (!matchingEvalTypes(EvalType.BOOL, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString()
                        + "' for unary operator '" + op.lexeme + "'; use 'BOOL'.");
                }
                return new Expr.Unary(op, right, EvalType.BOOL);
            }

            if (match(TokenType.MINUS))
            {
                Token op = previous();
                Expr right = unary();
                if (!matchingEvalTypes(EvalType.FLOAT, right.evalType))
                {
                    error(op, "Unexpected type '" + right.evalType.ToString() 
                        + "' for unary operator '" + op.lexeme + "'; use 'INT' or 'FLOAT'.");
                }
                return new Expr.Unary(op, right, (right.evalType == EvalType.TYPELESS) ? right.evalType:right.evalType);
            }

            return primary();
        }

        private Expr primary()
        {
            if (match(TokenType.FALSE)) return new Expr.Literal(false, EvalType.BOOL, previous());
            if (match(TokenType.TRUE)) return new Expr.Literal(true, EvalType.BOOL, previous());
            if (match(TokenType.NIL)) return new Expr.Literal(null, EvalType.NIL, previous());

            if (match(TokenType.INTEGER, TokenType.FLOAT_L, TokenType.STRING))
            {
                return new Expr.Literal(previous().literal, tokenToEvalType(previous().type), previous());
            }

            if (match(TokenType.IDENTIFIER))
            {
                TypedObject type;
                Token name = previous();
                if (match(TokenType.LEFT_PAREN))
                {
                    if (environment.getVar(new Token(name.type, name.lexeme + "(", name.literal, name.line), true).varType == EvalType.NIL)
                    {
                        throw error(name, "Function '" + name.lexeme + "' not defined.");
                    }
                    type = (TypedObject)environment.getVar(new Token(name.type, name.lexeme + "(", name.literal, name.line), true);
                    // TODO: Fix bodged fix for forcing name to be a part of the function instead of allowing currying and complex callees
                    return finishCall(new Expr.Variable(name, type.varType), (List<Parameter>)type.value);
                }
                if (environment.getVar(name, true).varType == EvalType.NIL)
                {
                   error(name, "Variable '" + name.lexeme + "' not defined.");
                }
                type = (TypedObject)environment.getVar(name, true);
                return new Expr.Variable(previous(), type.varType);
            }

            if (match(TokenType.LEFT_PAREN))
            {
                Expr expr = expression();
                consume(TokenType.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr, expr.evalType);
            }

            throw error(peek(), "Expect expression.");
        }

        private Expr call()
        {
            Expr expr = primary();
            Token name = previous();

            while (true)
            {
                if (match(TokenType.LEFT_PAREN))
                {
                    expr = finishCall(expr, null);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr finishCall(Expr callee, List<Parameter> parameters)
        {
            List<Expr> arguments = new List<Expr>();
            if (!check(TokenType.RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count >= 16)
                    {
                        error(peek(), "Cannot have more that 16 arguments.");
                    }
                    arguments.Add(expression());
                } while (match(TokenType.COMMA));
            }

            Token paren = consume(TokenType.RIGHT_PAREN, "Expect ')' after arguments.");

            // TODO: Support overloading
            if (parameters.Count != arguments.Count)
            {
                throw error(((Expr.Variable)callee).name, "Invalid number of arguments: expecting " + parameters.Count.ToString() +
                    ", received " + arguments.Count.ToString() + ".");
            }

            for (int i = 0; i < parameters.Count; i++)
            {
                if (!matchingEvalTypes(parameters[i].varType, arguments[i].evalType))
                {
                    error(((Expr.Variable)callee).name, "Argument " + (i + 1).ToString() + " type mismatch: expecting '" +
                        parameters[i].varType.ToString() + "', received '" + arguments[i].evalType.ToString() + "'.");
                }
            }

            return new Expr.Call(callee, paren, arguments, ((Expr.Variable)callee).evalType);
        }

        private Token consume(TokenType type, string message)
        {
            if (check(type)) return advance();

            throw error(peek(), message);
        }

        public ParseError error(Token token, string message)
        {
            AutonoCy_Main.error(token, message);
            return new ParseError();
        }

        private void syncronize()
        {
            advance();

            while (!isAtEnd())
            {
                if (previous().type == TokenType.SEMICOLON) return;

                switch (peek().type)
                {
                    case TokenType.CLASS:
                    case TokenType.FUN:
                    case TokenType.INT:
                    case TokenType.FLOAT:
                    case TokenType.STR:
                    case TokenType.FOR:
                    case TokenType.IF:
                    case TokenType.WHILE:
                    case TokenType.PRINT:
                    case TokenType.PRINT_ERR:
                    case TokenType.VOID:
                    case TokenType.BOOL:
                    case TokenType.PUBLIC:
                    case TokenType.PRIVATE:
                        return;
                }

                advance();
            }
        }

        public class ParseError : SystemException { }



        private bool match(params TokenType[] types)
        {
            foreach (TokenType type in types)
            {
                if (check(type))
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        private bool check(TokenType type)
        {
            if (isAtEnd()) return false;
            return peek().type == type;
        }

        private Token advance()
        {
            if (!isAtEnd()) current++;
            return previous();
        }

        private bool isAtEnd()
        {
            return peek().type == TokenType.EOF;
        }

        private Token peek()
        {
            return tokens[current];
        }

        private Token peekNext()
        {
            return tokens[current + 1];
        }

        private Token previous()
        {
            return tokens[current - 1];
        }

        private EvalType tokenToEvalType(TokenType type)
        {
            switch (type)
            {
                case TokenType.BOOL:
                case TokenType.TRUE:
                case TokenType.FALSE:
                    return EvalType.BOOL;
                case TokenType.FLOAT:
                case TokenType.FLOAT_L:
                    return EvalType.FLOAT;
                case TokenType.INT:
                case TokenType.INTEGER:
                    return EvalType.INT;
                case TokenType.STR:
                case TokenType.STRING:
                    return EvalType.STRING;
                case TokenType.VOID:
                    return EvalType.VOID;
                case TokenType.VAR:
                case TokenType.FUN:
                    return EvalType.TYPELESS;
                default:
                    return EvalType.NIL;
            }
        }

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
