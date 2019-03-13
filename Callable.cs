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

        public abstract object CALL(Interpreter interpreter, List<object> arguments);
    }

    class ClockNativeFunction : Callable
    {
        public override int arity { get; } = 0;

        public override object CALL(Interpreter interpreter, List<object> arguments)
        {
            return (double)DateTime.Now.TimeOfDay.TotalMilliseconds;
        }
    }

    class InputNativeFunction : Callable
    {
        public override int arity { get; } = 0;

        public override object CALL(Interpreter interpreter, List<object> arguments)
        {
            return Console.In.ReadLine();
        }
    }

    class StringToNumberNativeFunction : Callable
    {
        public override int arity { get; } = 1;

        public override object CALL(Interpreter interpreter, List<object> arguments)
        {
            if (arguments[0] is string)
            {
                try
                {
                    return Double.Parse((string)arguments[0]);
                }
                catch (FormatException e)
                {
                    AutonoCy_Main.runtimeError(new RuntimeError(new Token(TokenTypes.IDENTIFIER, "stringToNumber", null, -1), "stringToNumber - Unexpected formatting"));
                    return null;
                }
            }
            else
            {
                throw new RuntimeError(new Token(TokenTypes.IDENTIFIER, "stringToNumber", null, -1), "stringToNumber - unexpected argument type '" + arguments[0].GetType().ToString() + "', expecting type 'string'");
            }

        }
    }

    class ToStringNativeFunction : Callable
    {
        public override int arity { get; } = 1;

        public override object CALL(Interpreter interpreter, List<object> arguments)
        {
            return arguments[0].ToString();
        }
    }
}
