using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Autofac.Builder;
using Autofac.Core;

namespace DelegateInjections
{
    [AttributeUsage(AttributeTargets.Parameter)]
    public class InjectAttribute : Attribute
    {
        
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DelegateAttribute : Attribute
    {
        public DelegateAttribute(Type delegateType)
        {
            DelegateType = delegateType;
        }

        public Type DelegateType { get; }
    }

    public static class RegisterDelegates
    {
        public static IRegistrationBuilder<TResult, SimpleActivatorData, SingleRegistrationStyle>
            RegisterDelegate<TArg, TResult>(
                this ContainerBuilder builder,
                Func<TArg, TResult> delegateFactory)
        {
            return builder.Register(ctx => delegateFactory(
                arg: ctx.Resolve<TArg>()));
        }

        public static IRegistrationBuilder<TResult, SimpleActivatorData, SingleRegistrationStyle>
            RegisterDelegate<TArg1, TArg2, TResult>(
                this ContainerBuilder builder,
                Func<TArg1, TArg2, TResult> delegateFactory)
        {
            return builder.Register(ctx => delegateFactory(
                arg1: ctx.Resolve<TArg1>(), 
                arg2: ctx.Resolve<TArg2>()));
        }

        public static IRegistrationBuilder<TResult, SimpleActivatorData, SingleRegistrationStyle>
            RegisterDelegate<TArg1, TArg2, TArg3, TResult>(
                this ContainerBuilder builder,
                Func<TArg1, TArg2, TArg3, TResult> delegateFactory)
        {
            return builder.Register(ctx => delegateFactory(
                arg1: ctx.Resolve<TArg1>(),
                arg2: ctx.Resolve<TArg2>(),
                arg3: ctx.Resolve<TArg3>()));
        }

        public static void DiscoverDelegates<T>(this ContainerBuilder builder)
        {
            foreach (var method in GetDelegateMethods<T>())
            {
                var attribute = GetDelegateAttribute(method);

                builder.Register(ctx =>
                {
                    var arguments = ReplaceInjectableArgumentsWithResolvedConstants(method, ctx);

                    var injectableArgumentsInClosure = Expression.Invoke(
                        expression: Expression.Constant(ConvertMethodToDelegate(method)),
                        arguments: arguments);

                    var nonInjectableParameters = arguments
                        .Where(x => x is ParameterExpression)
                        .Cast<ParameterExpression>()
                        .ToArray();

                    return (dynamic) Expression.Lambda(
                        delegateType: attribute.DelegateType,
                        body: injectableArgumentsInClosure,
                        parameters: nonInjectableParameters).Compile();
                }).As(attribute.DelegateType);
            }
        }

        private static Expression[] ReplaceInjectableArgumentsWithResolvedConstants(
            MethodInfo method, 
            IComponentContext ctx)
        {
            return method
                .GetParameters()
                .Select(parameter => IsParameterInjectable(parameter)
                    ? (Expression) Expression.Constant(ctx.Resolve(parameter.ParameterType))
                    : Expression.Parameter(parameter.ParameterType, parameter.Name)).ToArray();
        }

        private static bool IsParameterInjectable(ParameterInfo parameter)
        {
            return parameter.GetCustomAttributes(typeof(InjectAttribute)).Any();
        }

        private static MethodInfo[] GetDelegateMethods<T>()
        {
            return typeof(T)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .Where(method => GetDelegateAttribute(method) != null)
                .ToArray();
        }

        private static DelegateAttribute GetDelegateAttribute(MethodInfo method)
        {
            var attributes = method.GetCustomAttributes(typeof(DelegateAttribute)).ToArray();

            if (attributes.Any())
            {
                return attributes.Single() as DelegateAttribute;
            }

            return null;
        }

        private static Delegate ConvertMethodToDelegate(MethodInfo method)
        {
            var parameters = method
               .GetParameters()
               .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
               .ToArray();

            var call = Expression.Call(method, parameters);
            return Expression.Lambda(call, parameters).Compile();
        }
    }
}
