using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    public enum TokenTypes
    {
        // Single character tokens
        LEFT_PAREN, RIGHT_PAREN, LEFT_BRACE, RIGHT_BRACE, LEFT_BRACKET, RIGHT_BRACKET,
        COMMA, DOT, MINUS, PLUS, SEMICOLON, SLASH, STAR, CARET,

        // 1-2 character tokens
        BANG, BANG_EQUAL, EQUAL, EQUAL_EQUAL,
        GREATER, GREATER_EQUAL, LESS, LESS_EQUAL,

        // Literals
        IDENTIFIER, STRING, INTEGER, FLOAT_L,

        // Keywords
        AND, BOOL, CLASS, ELSE, FALSE, FLOAT, FOR, FROM, FUN, IF, INT, NIL, 
        OR, PRINT, PRINT_ERR, PRIVATE, PUBLIC, RETURN, SUPER, STR, THIS, TRUE,
        VAR, VOID, WHILE,

        EOF
    }
}
