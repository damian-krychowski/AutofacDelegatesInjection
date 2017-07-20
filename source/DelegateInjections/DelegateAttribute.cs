using System;

namespace DelegateInjections
{
    [AttributeUsage(AttributeTargets.Method)]
    public class DelegateAttribute : Attribute
    {
        public DelegateAttribute(Type delegateType)
        {
            DelegateType = delegateType;
        }

        public Type DelegateType { get; }
    }
}