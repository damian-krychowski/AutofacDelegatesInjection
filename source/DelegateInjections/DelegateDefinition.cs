using System;
using System.Reflection;

namespace DelegateInjections
{
    internal class DelegateDefinition
    {
        public DelegateDefinition(Type delegateType, MethodInfo delegateMethod)
        {
            DelegateType = delegateType;
            DelegateMethod = delegateMethod;
        }

        public Type DelegateType { get; }
        public MethodInfo DelegateMethod { get; }

        public Type[] GenericTypeParameters => ((TypeInfo) DelegateType).GenericTypeParameters;
        public MethodInfo MakeGenericMethod(Type[] arguments) => DelegateMethod.MakeGenericMethod(arguments);
    }
}