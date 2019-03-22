using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Environment
    {
        public readonly Environment enclosing;
        private readonly Dictionary<string, TypedObject> values = new Dictionary<string, TypedObject>();

        public Environment()
        {
            enclosing = null;
        }

        public Environment(Environment enclosing)
        {
            this.enclosing = enclosing;
        }

        public TypedObject getVar(Token name, bool isParsing = false, bool localOnly = false)
        {
            if (values.ContainsKey(name.lexeme))
            {
                if (isParsing)
                {
                    return values[name.lexeme];
                }
                else
                {
                    return values[name.lexeme].GetValue();
                }
            }

            if (enclosing != null && !localOnly) return enclosing.getVar(name, isParsing);
            
            if (isParsing)
            {
                return new TypedObject(EvalType.NIL, null, null);
            }
            
            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        public void assign(Token name, TypedObject value)
        {
            if (values.ContainsKey(name.lexeme))
            {
                values[name.lexeme].SetValue(value, name);
                return;
            }

            if (enclosing != null)
            {
                enclosing.assign(name, value);
                return;
            }

            throw new RuntimeError(name, "Undefined variable '" + name.lexeme + "'.");
        }

        public void define (Token name, TypedObject value, bool parsing = false, Parser parser = null)
        {
            // Check type before just allowing it to be overwritten
            if (values.ContainsKey(name.lexeme))
            {
                if (parsing) {
                    if (name.lexeme.Last() == '(')
                    {
                        throw parser.error(name, "Function '" + name.lexeme.Substring(0, name.lexeme.Length - 1) + "' already defined in this scope.");
                    }
                    else
                    {
                        throw parser.error(name, "Variable '" + name.lexeme + "' already defined in this scope.");
                    }
                }
                else throw new RuntimeError(name, "Variable '" + name.lexeme + "' already defined in this scope.");
            }
            values[name.lexeme] = value;
        }
    }
}
