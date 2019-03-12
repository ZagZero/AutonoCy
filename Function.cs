using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Function : Callable
    {
        public override int arity { get; }

        private readonly Stmt.Function declaration;
        public Function(Stmt.Function declaration)
        {
            arity = declaration.parameters.Count();
            this.declaration = declaration;
        }

        public override object CALL(Interpreter interpreter, List<object> arguments)
        {
            Environment environment = new Environment(interpreter.globals);

            for (int i = 0; i < declaration.parameters.Count(); i++)
            {
                environment.define(declaration.parameters[i].lexeme, arguments[i]);
            }

            interpreter.executeBlock(declaration.body, environment);
            return null;
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
