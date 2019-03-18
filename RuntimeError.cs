using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutonoCy
{
    public class RuntimeError : SystemException
    {
        public readonly Token token;

        public RuntimeError (Token token, string message) : base(message)
        {
            this.token = token;
        }
    }

    public class Return : SystemException
    {
        public readonly EvalType type;
        public readonly object value;

        public Return(EvalType type, object value) : base()
        {
            this.type = type;
            this.value = value;
        }

        public TypedObject ToTypedObject()
        {
            return new TypedObject(type, value);
        }
    }
}
