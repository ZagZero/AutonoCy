using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Parser
    {
        private readonly List<Token> tokens;
        private int current = 0;

        public Parser(List<Token> tokens)
        {
            this.tokens = tokens;
        }

        public List<Stmt> parse()
        {
            List<Stmt> statements = new List<Stmt>();
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
                if (match(TokenTypes.FUN)) return function("function");
                if (match(TokenTypes.INT, TokenTypes.FLOAT, TokenTypes.STRING, TokenTypes.BOOL, TokenTypes.VAR))
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
            Token name = consume(TokenTypes.IDENTIFIER, "Expect " + kind + " name.");
            consume(TokenTypes.LEFT_PAREN, "Expect '(' after " + kind + " name.");
            List<Token> parameters = new List<Token>();

            if (!check(TokenTypes.RIGHT_PAREN))
            {
                do
                {
                    if (parameters.Count() >= 16)
                    {
                        error(peek(), "Cannot have more than 16 parameters.");
                    }

                    parameters.Add(consume(TokenTypes.IDENTIFIER, "Expected parameter name."));
                } while (match(TokenTypes.COMMA));
            }
            consume(TokenTypes.RIGHT_PAREN, "Expect ')' after parameters.");

            consume(TokenTypes.LEFT_BRACE, "Expect '{' before " + kind + " body.");
            List<Stmt> body = block();
            return new Stmt.Function(name, parameters, body);
        }

        private Stmt varDeclaration()
        {
            Token typeHold = previous();
            Token name = consume(TokenTypes.IDENTIFIER, "Expect variable name.");

            Expr initializer = null;
            if (match(TokenTypes.EQUAL))
            {
                initializer = expression();
            }

            consume(TokenTypes.SEMICOLON, "Expect ';' after variable declaration");
            switch (typeHold.type)
            {
                case TokenTypes.BOOL:
                    return new Stmt.Bool(name, initializer);
                case TokenTypes.FLOAT:
                    return new Stmt.Float(name, initializer);
                case TokenTypes.INT:
                    return new Stmt.Int(name, initializer);
                case TokenTypes.STRING:
                    return new Stmt.String(name, initializer);
                case TokenTypes.VAR:
                    return new Stmt.Var(name, initializer);
                default:
                    throw error(typeHold, "Unhandled type, interpreter shouldn't let this happen.");
            }
        }

        private Stmt statement()
        {
            if (match(TokenTypes.FOR)) return forStatement();
            if (match(TokenTypes.IF)) return ifStatement();
            if (match(TokenTypes.PRINT)) return printStatement(false);
            if (match(TokenTypes.PRINT_ERR)) return printStatement(true);
            if (match(TokenTypes.RETURN)) return returnStatement();
            if (match(TokenTypes.WHILE)) return whileStatement();
            if (match(TokenTypes.LEFT_BRACE)) return new Stmt.Block(block());

            return expressionStatement();
        }

        private Stmt forStatement()
        {
            consume(TokenTypes.LEFT_PAREN, "Expect '(' after 'for'.");

            Stmt initializer;
            if (match(TokenTypes.SEMICOLON))
            {
                initializer = null;
            }
            else if (match(TokenTypes.INT, TokenTypes.FLOAT, TokenTypes.VAR))
            {
                initializer = varDeclaration();
            }
            else
            {
                initializer = expressionStatement();
            }

            Expr condition = null;
            if (!check(TokenTypes.SEMICOLON))
            {
                condition = expression();
            }
            consume(TokenTypes.SEMICOLON, "Expect ';' after loop condition.");

            Expr increment = null;
            if (!check(TokenTypes.RIGHT_PAREN))
            {
                increment = expression();
            }
            consume(TokenTypes.RIGHT_PAREN, "Expect ')' after for clauses.");

            Stmt body = statement();

            if (increment != null)
            {
                body = new Stmt.Block(new List<Stmt> { body, new Stmt.Expression(increment)});
            }

            if (condition == null) condition = new Expr.Literal(true);
            body = new Stmt.While(condition, body);

            if (initializer != null)
            {
                body = new Stmt.Block(new List<Stmt> { initializer, body });
            }

            return body;
        }

        private Stmt ifStatement()
        {
            consume(TokenTypes.LEFT_PAREN, "Expect 'C' after 'if'.");
            Expr condition = expression();
            consume(TokenTypes.RIGHT_PAREN, "Expect ')' after if condition.");

            Stmt thenBranch = statement();
            Stmt elseBranch = null;
            if (match(TokenTypes.ELSE))
            {
                elseBranch = statement();
            }

            return new Stmt.If(condition, thenBranch, elseBranch);
        }

        private Stmt whileStatement()
        {
            consume(TokenTypes.LEFT_PAREN, "Expect '(' after 'while'.");
            Expr condition = expression();
            consume(TokenTypes.RIGHT_PAREN, "Expect ')' after while condition.");
            Stmt body = statement();

            return new Stmt.While(condition, body);
        }

        private Stmt printStatement(bool err)
        {
            Expr value = expression();
            consume(TokenTypes.SEMICOLON, "Expect ';' after value.");
            if (err) return new Stmt.Print_Err(value);
            return new Stmt.Print(value);
        }

        private Stmt expressionStatement()
        {
            Expr value = expression();
            consume(TokenTypes.SEMICOLON, "Expect ';' after value.");
            return new Stmt.Expression(value);
        }

        private Stmt returnStatement()
        {
            Token keyword = previous();
            Expr value = null;
            if (!check(TokenTypes.SEMICOLON))
            {
                value = expression();
            }

            consume(TokenTypes.SEMICOLON, "Expect ';' after return value.");
            return new Stmt.Return(keyword, value);
        }

        private List<Stmt> block()
        {
            List<Stmt> statements = new List<Stmt>();

            while (!check(TokenTypes.RIGHT_BRACE) && !isAtEnd())
            {
                statements.Add(declaration());
            }

            consume(TokenTypes.RIGHT_BRACE, "Expect '}' after block.");
            return statements;
        }

        private Expr assignment()
        {
            Expr expr = or();

            if (match(TokenTypes.EQUAL))
            {
                Token equals = previous();
                Expr value = assignment();

                if (expr.GetType() == typeof(Expr.Variable))
                {
                    Token name = ((Expr.Variable)expr).name;
                    return new Expr.Assign(name, value);
                }

                error(equals, "Invalid assignment target.");
            }

            return expr;
        }

        private Expr or()
        {
            Expr expr = and();

            while (match(TokenTypes.OR))
            {
                Token op = previous();
                Expr right = and();
                expr = new Expr.Logical(expr, op, right);
            }

            return expr;
        }

        private Expr and()
        {
            Expr expr = equality();

            while (match(TokenTypes.AND))
            {
                Token op = previous();
                Expr right = equality();
                expr = new Expr.Logical(expr, op, right);
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

            while (match(TokenTypes.BANG_EQUAL, TokenTypes.EQUAL_EQUAL))
            {
                Token op = previous();
                Expr right = comparison();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr comparison()
        {
            Expr expr = addition();

            while (match(TokenTypes.GREATER, TokenTypes.GREATER_EQUAL, TokenTypes.LESS, TokenTypes.LESS_EQUAL))
            {
                Token op = previous();
                Expr right = addition();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr addition()
        {
            Expr expr = multiplication();

            while (match(TokenTypes.MINUS, TokenTypes.PLUS))
            {
                Token op = previous();
                Expr right = multiplication();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr multiplication()
        {
            Expr expr = exponent();

            while (match(TokenTypes.SLASH, TokenTypes.STAR))
            {
                Token op = previous();
                Expr right = exponent();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr exponent()
        {
            Expr expr = unary();

            while (match(TokenTypes.CARET))
            {
                Token op = previous();
                Expr right = unary();
                expr = new Expr.Binary(expr, op, right);
            }

            return expr;
        }

        private Expr unary()
        {
            if (match(TokenTypes.BANG, TokenTypes.MINUS))
            {
                Token op = previous();
                Expr right = unary();
                return new Expr.Unary(op, right);
            }

            return call();
        }

        private Expr finishCall(Expr callee)
        {
            List<Expr> arguments = new List<Expr>();
            if (!check(TokenTypes.RIGHT_PAREN))
            {
                do
                {
                    if (arguments.Count >= 16)
                    {
                        error(peek(), "Cannot have more that 16 arguments.");
                    }
                    arguments.Add(expression());
                } while (match(TokenTypes.COMMA));
            }

            Token paren = consume(TokenTypes.RIGHT_PAREN, "Expect ')' after arguments.");

            return new Expr.Call(callee, paren, arguments);
        }

        private Expr call()
        {
            Expr expr = primary();

            while (true)
            {
                if (match(TokenTypes.LEFT_PAREN))
                {
                    expr = finishCall(expr);
                }
                else
                {
                    break;
                }
            }

            return expr;
        }

        private Expr primary()
        {
            if (match(TokenTypes.FALSE)) return new Expr.Literal(false);
            if (match(TokenTypes.TRUE)) return new Expr.Literal(true);
            if (match(TokenTypes.NIL)) return new Expr.Literal(null);

            if (match(TokenTypes.INTEGER, TokenTypes.FLOAT_L, TokenTypes.STRING))
            {
                return new Expr.Literal(previous().literal);
            }

            if (match(TokenTypes.IDENTIFIER))
            {
                return new Expr.Variable(previous());
            }

            if (match(TokenTypes.LEFT_PAREN))
            {
                Expr expr = expression();
                consume(TokenTypes.RIGHT_PAREN, "Expect ')' after expression.");
                return new Expr.Grouping(expr);
            }

            throw error(peek(), "Expect expression.");
        }

        private Token consume(TokenTypes type, string message)
        {
            if (check(type)) return advance();

            throw error(peek(), message);
        }

        private ParseError error(Token token, string message)
        {
            AutonoCy_Main.error(token, message);
            return new ParseError();
        }

        private void syncronize()
        {
            advance();

            while (!isAtEnd())
            {
                if (previous().type == TokenTypes.SEMICOLON) return;

                switch (peek().type)
                {
                    case TokenTypes.CLASS:
                    case TokenTypes.FUN:
                    case TokenTypes.INT:
                    case TokenTypes.FLOAT:
                    case TokenTypes.STR:
                    case TokenTypes.FOR:
                    case TokenTypes.IF:
                    case TokenTypes.WHILE:
                    case TokenTypes.PRINT:
                    case TokenTypes.PRINT_ERR:
                    case TokenTypes.VOID:
                    case TokenTypes.BOOL:
                    case TokenTypes.PUBLIC:
                    case TokenTypes.PRIVATE:
                        return;
                }

                advance();
            }
        }

        private class ParseError : SystemException { }



        private bool match(params TokenTypes[] types)
        {
            foreach (TokenTypes type in types)
            {
                if (check(type))
                {
                    advance();
                    return true;
                }
            }

            return false;
        }

        private bool check(TokenTypes type)
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
            return peek().type == TokenTypes.EOF;
        }

        private Token peek()
        {
            return tokens[current];
        }

        private Token previous()
        {
            return tokens[current - 1];
        }
    }
}
