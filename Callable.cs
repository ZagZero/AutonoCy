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
}
