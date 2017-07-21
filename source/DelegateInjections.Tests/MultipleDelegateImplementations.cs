using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using NUnit.Framework;

namespace DelegateInjections.Tests
{
    public delegate string SayHello();

    public class Greetings
    {
        [Delegate(typeof(SayHello))]
        public static string ShortHello()
        {
            return "Hi!";
        }

        [Delegate(typeof(SayHello))]
        public static string MediumHello()
        {
            return "Hello!";
        }

        [Delegate(typeof(SayHello))]
        public static string LongHello()
        {
            return "Good morning!";
        }
    }

    public class MultipleDelegateImplementations
    {
        [Test]
        public void can_resolve_each_implementation_of_delegate()
        {
            //given
            var builder = new ContainerBuilder();
            builder.DiscoverDelegates<Greetings>();
            var container = builder.Build();

            //when
            var greetings = container.Resolve<IEnumerable<SayHello>>();

            //then
            greetings.Select(sayHello => sayHello()).ShouldAllBeEquivalentTo(new[]
            {
                Greetings.ShortHello(),
                Greetings.MediumHello(),
                Greetings.LongHello()
            });
        }
    }
}
