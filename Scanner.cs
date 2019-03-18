using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace AutonoCy
{
    class Scanner
    {
        private readonly string source;
        private readonly List<Token> tokens = new List<Token>();
        private int start = 0;
        private int current = 0;
        private int line = 1;

        static readonly Dictionary<string, TokenType> keywords;

        private bool isAtEnd()
        {
            return current >= source.Length;
        }

        static Scanner()
        {
            keywords = new Dictionary<string, TokenType>();
            keywords.Add("and", TokenType.AND);
            keywords.Add("bool", TokenType.BOOL);
            keywords.Add("class", TokenType.CLASS);
            keywords.Add("else", TokenType.ELSE);
            keywords.Add("false", TokenType.FALSE);
            keywords.Add("float", TokenType.FLOAT);
            keywords.Add("from", TokenType.FROM);
            keywords.Add("for", TokenType.FOR);
            keywords.Add("fun", TokenType.FUN);
            keywords.Add("if", TokenType.IF);
            keywords.Add("int", TokenType.INT);
            keywords.Add("nil", TokenType.NIL);
            keywords.Add("or", TokenType.OR);
            keywords.Add("print", TokenType.PRINT);
            keywords.Add("printErr", TokenType.PRINT_ERR);
            keywords.Add("private", TokenType.PRIVATE);
            keywords.Add("public", TokenType.PUBLIC);
            keywords.Add("return", TokenType.RETURN);
            keywords.Add("string", TokenType.STR);
            keywords.Add("super", TokenType.SUPER);
            keywords.Add("this", TokenType.THIS);
            keywords.Add("true", TokenType.TRUE);
            keywords.Add("var", TokenType.VAR);
            keywords.Add("while", TokenType.WHILE);
        }


        public Scanner(string source)
        {
            this.source = source;


        }

        public List<Token> scanTokens()
        {
            while(!isAtEnd())
            {
                // At the beginning of next lexeme
                start = current;
                scanToken();
            }

            tokens.Add(new Token(TokenType.EOF, "", null, line));
            return tokens;
        }

        void scanToken()
        {
            char c = advance();
            switch (c)
            {
                // Single character lexemes
                case '(': addToken(TokenType.LEFT_PAREN); break;
                case ')': addToken(TokenType.RIGHT_PAREN); break;
                case '{': addToken(TokenType.LEFT_BRACE); break;
                case '}': addToken(TokenType.RIGHT_BRACE); break;
                case '[': addToken(TokenType.LEFT_BRACKET); break;
                case ']': addToken(TokenType.RIGHT_BRACKET); break;
                case ',': addToken(TokenType.COMMA); break;
                case '.': addToken(TokenType.DOT); break;
                case '-': addToken(TokenType.MINUS); break;
                case '+': addToken(TokenType.PLUS); break;
                case ';': addToken(TokenType.SEMICOLON); break;
                case '*': addToken(TokenType.STAR); break;
                case '^': addToken(TokenType.CARET); break;

                // Possibly double character lexemes
                case '!': addToken(match('=') ? TokenType.BANG_EQUAL : TokenType.BANG); break;
                case '=': addToken(match('=') ? TokenType.EQUAL_EQUAL : TokenType.EQUAL); break;
                case '<': addToken(match('=') ? TokenType.LESS_EQUAL : TokenType.LESS); break;
                case '>': addToken(match('=') ? TokenType.GREATER_EQUAL : TokenType.GREATER); break;
                case '&':
                    if (match('&'))
                    {
                        addToken(TokenType.AND);
                    }
                    else
                    {
                        AutonoCy_Main.error(line, "Unsupported operator. Did you mean '&&'?");
                    }
                    break;
                case '|':
                    if (match('|'))
                    {
                        addToken(TokenType.OR);
                    }
                    else
                    {
                        AutonoCy_Main.error(line, "Unsupported operator. Did you mean '||'?");
                    }
                    break;

                // Longer lexemes
                case '/':
                    if (match('/'))
                    {
                        // Ignore line because comment
                        while (peek() != '\n' && !isAtEnd()) advance();
                    }
                    else
                    {
                        addToken(TokenType.SLASH);
                    }
                    break;

                // Whitespaces
                case ' ':
                case '\r':
                case '\t':
                    // Ignore these
                    break;
                case '\n':
                    // Increment line count, but don't do anything else
                    line++;
                    break;

                // String Literal
                case '"': stringL(); break;

                default:
                    // Catch numbers here
                    if (isDigit(c))
                    {
                        numberL();
                    }
                    // Catch all Alphabetical things here
                    else if (isAlpha(c)) {
                        identifierL();
                    }
                    else
                    {
                        // Error from unexpected character
                        AutonoCy_Main.error(line, "Unexpected character.");
                    }
                    break;
            }
        }

        char advance()
        {
            current++;
            return source[current - 1];
        }

        void addToken(TokenType type)
        {
            addToken(type, null);
        }

        void addToken(TokenType type, Object literal)
        {
            string text = source.Substring(start, current - start);
            tokens.Add(new Token(type, text, literal, line));
        }

        bool match(char expected)
        {
            if (isAtEnd()) return false;
            if (source[current] != expected) return false;

            current++;
            return true;
        }

        char peek()
        {
            if (isAtEnd()) return '\0';
            return source[current];
        }

        char peekNext()
        {
            if (current + 1 >= source.Length) return '\0';
            return source[current + 1];
        }

        bool isDigit(char c)
        {
            return c >= '0' && c <= '9';
        }

        bool isAlpha(char c)
        {
            return (c >= 'a' && c <= 'z') ||
                   (c >= 'A' && c <= 'Z') ||
                   (c == '_');
        }

        bool isAlphaNumeric(char c)
        {
            return isAlpha(c) || isDigit(c);
        }
        
        void stringL()
        {
            char next;
            string stringLiteral = "";
            bool escape = false;

            next = peek();
            while (next != '"' && !isAtEnd())
            {
                if (next == '\\')
                {
                    escape = true;
                }
                if (next == '\n') line++;

                if (!escape) { stringLiteral = stringLiteral + next; }
                else
                {
                    if (!isAtEnd())
                    {
                        advance();
                        next = peek();
                        switch (next)
                        {
                            case 'n':
                                stringLiteral = stringLiteral + '\n';
                                break;
                            case '"':
                                stringLiteral = stringLiteral + '"';
                                break;
                            case '\\':
                                stringLiteral = stringLiteral + '\\';
                                break;
                            default:
                                AutonoCy_Main.error(line, "Unrecognized escape sequence.");
                                break;
                        }
                        escape = false;
                    }
                }

                advance();
                next = peek();
            }

            if (isAtEnd())
            {
                AutonoCy_Main.error(line, "Unterminated string.");
                return;
            }

            // Move past the closing "
            advance();

            // Trim the surrounding quotes
            string value = source.Substring(start + 1, current - start - 2);
            addToken(TokenType.STRING, stringLiteral);
        }

        void numberL()
        {
            bool isFloat = false;
            while (isDigit(peek())) advance();

            // Check for decimal
            if (peek() == '.' && isDigit(peekNext()))
            {
                // Flag it as a float
                isFloat = true;

                // Move past the .
                advance();

                while (isDigit(peek())) advance();
            }

            if (isFloat)
            {
                addToken(TokenType.FLOAT_L, double.Parse(source.Substring(start, current - start)));
            }
            else
            {
                addToken(TokenType.INTEGER, int.Parse(source.Substring(start, current - start)));
            }
        }

        void identifierL()
        {
            while (isAlphaNumeric(peek())) advance();

            string text = source.Substring(start, current - start);

            TokenType type;
            if (keywords.TryGetValue(text, out type))
            {
                addToken(type);
            }
            else
            {
                addToken(TokenType.IDENTIFIER);
            }
        }

    }
}
