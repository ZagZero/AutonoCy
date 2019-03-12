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

        static readonly Dictionary<string, TokenTypes> keywords;

        private bool isAtEnd()
        {
            return current >= source.Length;
        }

        static Scanner()
        {
            keywords = new Dictionary<string, TokenTypes>();
            keywords.Add("and", TokenTypes.AND);
            keywords.Add("bool", TokenTypes.BOOL);
            keywords.Add("class", TokenTypes.CLASS);
            keywords.Add("else", TokenTypes.ELSE);
            keywords.Add("false", TokenTypes.FALSE);
            keywords.Add("float", TokenTypes.FLOAT);
            keywords.Add("from", TokenTypes.FROM);
            keywords.Add("for", TokenTypes.FOR);
            keywords.Add("fun", TokenTypes.FUN);
            keywords.Add("if", TokenTypes.IF);
            keywords.Add("int", TokenTypes.INT);
            keywords.Add("nil", TokenTypes.NIL);
            keywords.Add("or", TokenTypes.OR);
            keywords.Add("print", TokenTypes.PRINT);
            keywords.Add("printErr", TokenTypes.PRINT_ERR);
            keywords.Add("private", TokenTypes.PRIVATE);
            keywords.Add("public", TokenTypes.PUBLIC);
            keywords.Add("return", TokenTypes.RETURN);
            keywords.Add("string", TokenTypes.STR);
            keywords.Add("super", TokenTypes.SUPER);
            keywords.Add("this", TokenTypes.THIS);
            keywords.Add("true", TokenTypes.TRUE);
            keywords.Add("while", TokenTypes.WHILE);
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

            tokens.Add(new Token(TokenTypes.EOF, "", null, line));
            return tokens;
        }

        void scanToken()
        {
            char c = advance();
            switch (c)
            {
                // Single character lexemes
                case '(': addToken(TokenTypes.LEFT_PAREN); break;
                case ')': addToken(TokenTypes.RIGHT_PAREN); break;
                case '{': addToken(TokenTypes.LEFT_BRACE); break;
                case '}': addToken(TokenTypes.RIGHT_BRACE); break;
                case '[': addToken(TokenTypes.LEFT_BRACKET); break;
                case ']': addToken(TokenTypes.RIGHT_BRACKET); break;
                case ',': addToken(TokenTypes.COMMA); break;
                case '.': addToken(TokenTypes.DOT); break;
                case '-': addToken(TokenTypes.MINUS); break;
                case '+': addToken(TokenTypes.PLUS); break;
                case ';': addToken(TokenTypes.SEMICOLON); break;
                case '*': addToken(TokenTypes.STAR); break;
                case '^': addToken(TokenTypes.CARET); break;

                // Possibly double character lexemes
                case '!': addToken(match('=') ? TokenTypes.BANG_EQUAL : TokenTypes.BANG); break;
                case '=': addToken(match('=') ? TokenTypes.EQUAL_EQUAL : TokenTypes.EQUAL); break;
                case '<': addToken(match('=') ? TokenTypes.LESS_EQUAL : TokenTypes.LESS); break;
                case '>': addToken(match('=') ? TokenTypes.GREATER_EQUAL : TokenTypes.GREATER); break;
                case '&':
                    if (match('&'))
                    {
                        addToken(TokenTypes.AND);
                    }
                    else
                    {
                        AutonoCy_Main.error(line, "Unsupported operator. Did you mean '&&'?");
                    }
                    break;
                case '|':
                    if (match('|'))
                    {
                        addToken(TokenTypes.OR);
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
                        addToken(TokenTypes.SLASH);
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

        void addToken(TokenTypes type)
        {
            addToken(type, null);
        }

        void addToken(TokenTypes type, Object literal)
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
            addToken(TokenTypes.STRING, stringLiteral);
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
                addToken(TokenTypes.FLOAT_L, Double.Parse(source.Substring(start, current - start)));
            }
            else
            {
                addToken(TokenTypes.INTEGER, Int32.Parse(source.Substring(start, current - start)));
            }
        }

        void identifierL()
        {
            while (isAlphaNumeric(peek())) advance();

            string text = source.Substring(start, current - start);

            TokenTypes type;
            if (keywords.TryGetValue(text, out type))
            {
                addToken(type);
            }
            else
            {
                addToken(TokenTypes.IDENTIFIER);
            }
        }

    }
}
