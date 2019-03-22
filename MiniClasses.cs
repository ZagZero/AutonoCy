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

        public TypedObject(EvalType varType, object value, Token errToken, bool overrideTyping = false)
        {
            this.varType = varType;
            SetValue(value, errToken, overrideTyping);
        }

        public TypedObject GetValue()
        {
            switch (varType)
            {
                case EvalType.FUNCTION:
                case EvalType.BOOL:
                case EvalType.FLOAT:
                case EvalType.INT:
                case EvalType.STRING:
                    return this;
                case EvalType.TYPELESS:
                    if (value is TypedObject)
                    {
                        return (TypedObject)value;
                    }
                    return new TypedObject(EvalType.NIL, null, null);
                case EvalType.NIL:
                    return new TypedObject(EvalType.NIL, null, null);
                default:
                    return new TypedObject(EvalType.NIL, null, null);
                    
            }
        }

        public void SetValue(object obj, Token errToken, bool overrideTyping = false)
        {
            if (varType == EvalType.FUNCTION)
            {
                value = obj;
                return;
            }
            TypedObject val = null;
            // If the passed object is TypedObject, then set val directly to it
            if (obj is TypedObject)
            {
                val = ((TypedObject)obj).GetValue();
                obj = val.value;
            }

            // If the variable type is typeless, it needs to store val directly
            if (varType == EvalType.TYPELESS)
            {
                if (!overrideTyping) SetTypecast(val);
                else value = obj;
                return;
            }

            if (!overrideTyping)
            {

                // Check if this variable's type matches the provided value's
                if (!matchingEvalTypes(varType, getObjectType(obj)))
                {
                    // Throw Runtime Error
                    throw new RuntimeError(errToken, "Type mismatch: Expected type '" + varType.ToString() +
                        "', recieved type '" + getObjectType(obj).ToString() + "'.");
                }

                SetTypecast(obj);
            }
            else
            {
                value = obj;
            }

        }

        private void SetTypecast (object obj)
        {
            if (obj != null)
            {
                switch (varType)
                {
                    case EvalType.TYPELESS:
                        ((TypedObject)obj).SetTypecast(((TypedObject)obj).value);
                        value = (TypedObject)obj;
                        return;
                    case EvalType.INT:
                        value = (int)((obj is double)? (double)obj : (int) obj);
                        return;
                    case EvalType.FLOAT:
                        value = (double)((obj is double) ? (double)obj : (int)obj);
                        return;
                    case EvalType.STRING:
                        value = (string)obj;
                        return;
                    case EvalType.BOOL:
                        value = (bool)obj;
                        return;
                }
            }
        }

        public static bool matchingEvalTypes(EvalType requiredType, EvalType compareType, bool warn = false, Token warnAtToken = null)
        {

            if (requiredType == EvalType.TYPELESS && compareType != EvalType.VOID) return true;
            if (compareType == EvalType.TYPELESS && requiredType != EvalType.VOID)
            {
                // Warn if warning is requested
                return true;
            }

            if ((requiredType == EvalType.INT || requiredType == EvalType.FLOAT) &&
                (compareType == EvalType.INT || compareType == EvalType.FLOAT))
            {
                return true;
            }

            return requiredType == compareType;
        }

        private EvalType getObjectType(object value)
        {
            if (value is float || value is double) { return EvalType.FLOAT; }
            if (value is int) { return EvalType.INT; }
            if (value is bool) { return EvalType.BOOL; }
            if (value is string) { return EvalType.STRING; }
            if (value is TypedObject)
            {
                if (((TypedObject)value).varType == EvalType.TYPELESS)
                {
                    TypedObject varVal = (TypedObject)((TypedObject)value).value;
                    if (varVal != null)
                        return ((TypedObject)((TypedObject)value).value).varType;
                    else
                        return EvalType.NIL;
                }
                else
                    return ((TypedObject)value).varType;
            }

            return EvalType.NIL;

        }

        public string ToString()
        {
            switch (varType)
            {
                case EvalType.BOOL:
                case EvalType.FLOAT:
                case EvalType.INT:
                case EvalType.STRING:
                    return value.ToString();
                case EvalType.FUNCTION:
                    if (value is Function)
                    {
                        return ((Function)value).ToString();
                    }
                    return "NIL";
                case EvalType.TYPELESS:
                    if (value is TypedObject)
                    {
                        return ((TypedObject)value).value.ToString();
                    }
                    return "NIL";
                case EvalType.NIL:
                    return "NIL";
                default:
                    return "NIL";

            }

        }
    }
}
