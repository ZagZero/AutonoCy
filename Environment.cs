using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Environment
    {
        readonly Environment enclosing;
        private readonly Dictionary<string, object> values = new Dictionary<string, object>();

        public Environment()
        {
            enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        public object getVar(Token name)
        {
            if (values.ContainsKey(name.lexeme))
            {
                return values[name.lexeme];
            }

            if (enclosing != null) return enclosing.getVar(name);

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        public void assign(Token name, object value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme] = value;
                return;
            }

            if (enclosing != null)
            {
                enclosing.assign(name, value);
                return;
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        public void define (string name, object value)
        {
            // Check type before just allowing it to be overwritten
            values[name] = value;
        }
    }
}
