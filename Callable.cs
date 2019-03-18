using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    abstract class Callable
    {
        public abstract int arity { get; }
        public abstract EvalType returnType { get; }

        public abstract TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments);
    }

    class ClockNativeFunction : Callable
    {
        public override int arity { get; } = 0;
        public override EvalType returnType { get; } = EvalType.FLOAT;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.FLOAT, (double)DateTime.Now.TimeOfDay.TotalMilliseconds);
        }
    }

    class InputNativeFunction : Callable
    {
        public override int arity { get; } = 0;
        public override EvalType returnType { get; } = EvalType.STRING;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.STRING, Console.In.ReadLine());
        }
    }

    class StringToNumberNativeFunction : Callable
    {
        public override int arity { get; } = 1;
        public override EvalType returnType { get; } = EvalType.FLOAT;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            if (arguments[0].value is string)
            {
                try
                {
                    return new TypedObject(EvalType.FLOAT, Double.Parse((string)arguments[0].value));
                }
                catch (FormatException e)
                {
                    AutonoCy_Main.runtimeError(new RuntimeError(new Token(TokenType.IDENTIFIER, "stringToNumber", null, -1), "stringToNumber - Unexpected formatting"));
                    return new TypedObject(EvalType.NIL, null);
                }
            }
            else
            {
                throw new RuntimeError(new Token(TokenType.IDENTIFIER, "stringToNumber", null, -1), "stringToNumber - unexpected argument type '" + arguments[0].GetType().ToString() + "', expecting type 'string'");
            }

        }
    }

    class ToStringNativeFunction : Callable
    {
        public override int arity { get; } = 1;
        public override EvalType returnType { get; } = EvalType.STRING;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.STRING, arguments[0].value.ToString());
        }
    }
}
