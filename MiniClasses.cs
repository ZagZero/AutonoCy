using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    public class Parameter
    {
        public readonly EvalType varType;
        public readonly Token name;

        public Parameter (EvalType varType, Token name)
        {
            this.varType = varType;
            this.name = name;
        }
    }

    public class TypedObject
    {
        public readonly EvalType varType;
        public object value;

        public TypedObject (EvalType varType, object value)
        {
            this.varType = varType;
            this.value = value;
        }
    }
}
