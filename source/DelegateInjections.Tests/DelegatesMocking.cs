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
    public delegate DateTime UtcNow();

    public class Timer
    {
        [Delegate(typeof(UtcNow))]
        public static DateTime SystemUtcNow()
        {
            return DateTime.UtcNow;
        }
    }

    public class DelegatesMocking
    {
        [Test]
        public void can_mock_delegate()
        {
            //given
            var builder = new ContainerBuilder();
            builder.DiscoverDelegates<Timer>();

            UtcNow mockUtcNow = () => new DateTime(1991, 2, 7);
            builder.RegisterDelegate(() => mockUtcNow).SingleInstance();

            var container = builder.Build();

            //when
            var utcNow = container.Resolve<UtcNow>();

            //then
            utcNow().Should().Be(new DateTime(1991, 2, 7));
        }
    }
}
