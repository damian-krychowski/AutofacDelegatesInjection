using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using NUnit.Framework;

namespace DelegateInjections.Tests
{
    public delegate int Add(int arg1, int arg2);

    public delegate int Multiply(int arg1, int arg2);

    public delegate int Power(int arg1, int arg2);

    public class FuncsToDiscover
    {
        [Delegate(typeof(Add))]
        public static int Add(int arg1, int arg2)
        {
            return arg1 + arg2;
        }

        [Delegate(typeof(Multiply))]
        public static int Multiply(int arg1, int arg2, [Inject] Add add)
        {
            var result = 0;

            for (int i = 0; i < arg2; i++)
            {
                result = add(arg1, result);
            }

            return result;
        }

        [Delegate(typeof(Power))]
        public static int Power(int arg1, int arg2, [Inject] Multiply multiply)
        {
            var result = 1;

            for (int i = 0; i < arg2; i++)
            {
                result = multiply(arg1, result);
            }

            return result;
        }
    }

    public class ExpressionTests
    {
      
        [Test]
        public void create_func_to_invoke_method_with_expression()
        {
            var method = typeof(ExpressionTests).GetMethod("Method", BindingFlags.Public | BindingFlags.Static);

            var parameters = method
                .GetParameters()
                .Select(parameter => Expression.Parameter(parameter.ParameterType, parameter.Name))
                .ToArray(); 
            
            var call = Expression.Call(method, parameters);
            var callFunc = Expression.Lambda(call, parameters).Compile();

            callFunc.DynamicInvoke("test").Should().Be("test");
        }



        public static string Method(string arg)
        {
            return arg;
        }
    }

    public class DelegateDiscovering
    {
        [Test]
        public void can_discover_and_use_funcs()
        {
            //given
            var builder = new ContainerBuilder();

            builder.DiscoverDelegates<FuncsToDiscover>();

            var container = builder.Build();
            
            //when
            var add = container.Resolve<Add>();
            var multiply = container.Resolve<Multiply>();
            var power = container.Resolve<Power>();

            //then
            add(1, 2).Should().Be(3);
            multiply(2, 3).Should().Be(6);
            power(2, 2).Should().Be(4);
        }
    }
}
