using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    public class Token
    {
        public readonly TokenTypes type;
        public readonly string lexeme;
        public readonly object literal;
        public readonly int line;

        public Token(TokenTypes type, string lexeme, object literal, int line)
        {
            this.type = type;
            this.lexeme = lexeme;
            this.literal = literal;
            this.line = line;
        }

        public string toString()
        {
            return type + " " + lexeme + " " + literal;
        }
    }
}
