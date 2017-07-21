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
using Autofac.Core.Activators.Delegate;
using Autofac.Core.Lifetime;
using Autofac.Core.Registration;

namespace DelegateInjections
{
    public static class RegisterDelegates
    {
        public static bool IsSubclassOfRawGeneric(this Type toCheck, Type generic)
        {
            while (toCheck != null && toCheck != typeof(object))
            {
                var cur = toCheck.IsGenericType ? toCheck.GetGenericTypeDefinition() : toCheck;
                if (generic == cur)
                {
                    return true;
                }
                toCheck = toCheck.BaseType;
            }
            return false;
        }

        public static IRegistrationBuilder<TResult, SimpleActivatorData, SingleRegistrationStyle>
           RegisterDelegate<TResult>(
               this ContainerBuilder builder,
               Func<TResult> delegateFactory)
        {
            return builder.Register(ctx => delegateFactory());
        }

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

        public static ContainerBuilder DiscoverDelegates<T>(this ContainerBuilder builder)
        {
            foreach (var method in GetDelegateMethods<T>())
            {
                var attribute = GetDelegateAttribute(method);

                if (attribute.DelegateType.IsGenericTypeDefinition)
                {
                    builder
                        .RegisterSource(new GenericDelegateRegistrationSource(
                            definition: new DelegateDefinition(
                                delegateType: attribute.DelegateType,
                                delegateMethod: method)));
                }
                else
                {
                    builder
                        .Register(ctx => BuildUpConcreteDelegate(ctx, method, attribute.DelegateType))
                        .As(attribute.DelegateType);
                }
            }

            return builder;
        }

        private static dynamic BuildUpConcreteDelegate(
            IComponentContext ctx,
            MethodInfo delegateImplementationMethod,
            Type delegateType)
        {
            var arguments = ReplaceInjectableArgumentsWithResolvedConstants(delegateImplementationMethod, ctx);

            var injectableArgumentsInClosure = Expression.Invoke(
                expression: Expression.Constant(ConvertMethodToDelegate(delegateImplementationMethod)),
                arguments: arguments);

            var nonInjectableParameters = arguments
                .Where(x => x is ParameterExpression)
                .Cast<ParameterExpression>()
                .ToArray();

            return Expression.Lambda(
                delegateType: delegateType,
                body: injectableArgumentsInClosure,
                parameters: nonInjectableParameters).Compile();
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

        internal class GenericDelegateRegistrationSource : IRegistrationSource
        {
            private readonly DelegateDefinition _definition;

            public GenericDelegateRegistrationSource(DelegateDefinition definition)
            {
                if (!definition.DelegateType.IsGenericTypeDefinition)
                {
                    throw new ArgumentException($"Type {_definition.DelegateType.FullName} is not open generic type!");
                }

                _definition = definition;
            }

            public IEnumerable<IComponentRegistration> RegistrationsFor(
                Service service,
                Func<Service, IEnumerable<IComponentRegistration>> registrationAccessor)
            {
                var swt = service as IServiceWithType;
                if (swt == null || !swt.ServiceType.IsSubclassOfRawGeneric(_definition.DelegateType))
                {
                    return Enumerable.Empty<IComponentRegistration>();
                }

                return new IComponentRegistration[] {new ComponentRegistration(
                    Guid.NewGuid(),
                    new DelegateActivator(swt.ServiceType, (ctx, p) => BuildUpConcreteDelegate(
                        ctx: ctx,
                        delegateImplementationMethod: _definition
                            .MakeGenericMethod(swt.ServiceType.GenericTypeArguments),
                        delegateType: swt.ServiceType)),
                    new CurrentScopeLifetime(),
                    InstanceSharing.None,
                    InstanceOwnership.OwnedByLifetimeScope,
                    new[] {service},
                    new Dictionary<string, object>())};
            }

            public bool IsAdapterForIndividualComponents => false;
        }
    }
}
