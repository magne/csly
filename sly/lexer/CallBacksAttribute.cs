using System;

namespace sly.lexer
{
    [AttributeUsage(AttributeTargets.Enum)]
    public class CallBacksAttribute : Attribute
    {
        public Type CallBacksClass { get; set; }

        public CallBacksAttribute(Type callBacksClass)
        {
            CallBacksClass = callBacksClass;
        }
    }
}