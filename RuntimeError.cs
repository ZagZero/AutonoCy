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
}
