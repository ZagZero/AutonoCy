using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    abstract class Callable
    {
        public abstract List<EvalType> paramTypes { get; }
        public abstract EvalType returnType { get; }

        public abstract TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments);
    }

    class ClockNativeFunction : Callable
    {
        public override List<EvalType> paramTypes { get; } = new List<EvalType> { };
        public override EvalType returnType { get; } = EvalType.FLOAT;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.FLOAT, (double)DateTime.Now.TimeOfDay.TotalMilliseconds, null);
        }
    }

    class InputNativeFunction : Callable
    {
        public override List<EvalType> paramTypes { get; } = new List<EvalType> { };
        public override EvalType returnType { get; } = EvalType.STRING;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.STRING, Console.In.ReadLine(), null);
        }
    }

    class StringToNumberNativeFunction : Callable
    {
        public override List<EvalType> paramTypes { get; } = new List<EvalType> { EvalType.STRING };
        public override EvalType returnType { get; } = EvalType.FLOAT;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            if (arguments[0].value is string)
            {
                try
                {
                    return new TypedObject(EvalType.FLOAT, Double.Parse((string)arguments[0].value), null);
                }
                catch (FormatException e)
                {
                    AutonoCy_Main.runtimeError(new RuntimeError(new Token(TokenType.IDENTIFIER, "stringToNumber", null, -1), "stringToNumber - Unexpected formatting"));
                    return new TypedObject(EvalType.NIL, null, null);
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
        public override List<EvalType> paramTypes { get; } = new List<EvalType> { EvalType.TYPELESS };
        public override EvalType returnType { get; } = EvalType.STRING;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.STRING, arguments[0].GetValue().ToString(), null);
        }
    }

    class GetTypeNativeFunction : Callable
    {
        public override List<EvalType> paramTypes { get; } = new List<EvalType> { EvalType.TYPELESS };
        public override EvalType returnType { get; } = EvalType.STRING;

        public override TypedObject CALL(Interpreter interpreter, List<TypedObject> arguments)
        {
            return new TypedObject(EvalType.STRING, arguments[0].GetValue().varType.ToString(), null);
        }

    }
}
