using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    class Function : Callable
    {
        public override List<EvalType> paramTypes { get; }
        public override EvalType returnType { get; }

        private readonly Stmt.Function declaration;
        public Function(Stmt.Function declaration)
        {
            paramTypes = new List<EvalType>();
            foreach (Parameter param in declaration.parameters)
            {
                paramTypes.Add(param.varType);
            }
            this.declaration = declaration;
        }

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            Environment environment = new Environment(interpreter.globals);

            for (int i = 0; i < declaration.parameters.Count(); i++)
            {
                environment.define(declaration.parameters[i].name, arguments[i]);
            }

            try
            {
                interpreter.executeBlock(declaration.body, environment);
            }
            catch (Return returnValue)
            {
                return returnValue.ToTypedObject();
            }
            return null;
        }

        public override string ToString()
        {
            return "<fn " + declaration.name.lexeme + ">";
        }
    }
}
