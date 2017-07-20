using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using NUnit.Framework;
using NUnit.Framework.Constraints;

namespace DelegateInjections.Tests
{
    public delegate T Pop<T>();

    public delegate void Push<T1, T2>(T1 first, T2 second);

    public class Valut<T>
    {
        public List<T> Items { get; } = new List<T>();
    }

    public class GenericDelegates
    {
        [Delegate(typeof(Push<,>))]
        public static void Push<T1, T2>(T1 first, T2 second, 
            [Inject] Valut<T1> firstVault,
            [Inject] Valut<T2> secondVault)
        {
            firstVault.Items.Add(first);
            secondVault.Items.Add(second);
        }

        [Delegate(typeof(Pop<>))]
        public static T Pop<T>([Inject] Valut<T> vault)
        {
            var item = vault.Items.Last();
            vault.Items.RemoveAt(vault.Items.Count - 1);

            return item;
        }
    }

    public class GenericDelegatesTests
    {
        [Test]
        public void can_resolve_generic_delegates()
        {
            //given
            var builder = new ContainerBuilder();
            builder.RegisterGeneric(typeof(Valut<>)).SingleInstance();
            builder.DiscoverDelegates<GenericDelegates>();

            var container = builder.Build();

            //when
            var push = container.Resolve<Push<int,string>>();

            var intPop = container.Resolve<Pop<int>>();
            var stringPop = container.Resolve<Pop<string>>();

            //then
            push(123, "test");

            intPop().Should().Be(123);
            stringPop().Should().Be("test");
        }
    }
}
