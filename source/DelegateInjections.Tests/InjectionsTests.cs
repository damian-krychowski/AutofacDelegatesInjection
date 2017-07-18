using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.SqlServer.Server;
using NUnit.Framework;

namespace DelegateInjections.Tests
{
    public class StringStore
    {
        public string Value { get; set; }

    }

    public delegate void WriteValue(string firstArg);
    
    public delegate string AppendValue(string firstArg);
    
    public delegate string ConcatValues(string first, string second);
    
    public static class Funcs
    {
        public static Func<StringStore, WriteValue> WriteValue { get; } =
            (stringStore) => (arg) => Write(arg, stringStore);
        
        public static void Write(string arg, StringStore store)
        {
            store.Value = arg;
        }
        
        public static Func<StringStore, AppendValue> AppendValue { get; } =
            (stringStore) => (firsArg) => Append(firsArg, stringStore);

        public static string Append(string arg, StringStore store)
        {
            return store.Value + " " + arg;
        }


        public static Func<WriteValue, AppendValue, ConcatValues> ConcatValues { get; } =
            (write, append) => (arg1, arg2) => Concat(arg1, arg2, write, append);

        public static string Concat(string arg1, string arg2, WriteValue write, AppendValue append)
        {
            write(arg1);
            return append(arg2);
        }

    }

    public class DelegatesDependendClass
    {
        private readonly WriteValue _writeValue;
        private readonly AppendValue _appendValue;

        public DelegatesDependendClass(
            WriteValue writeValue,
            AppendValue appendValue)
        {
            _writeValue = writeValue;
            _appendValue = appendValue;
        }

        public string AddText(string first, string second)
        {
            _writeValue(first);
            return _appendValue(second);
        }
    }

    public class InjectionsTests
    {
        [Test]
        public void can_append_texts()
        {
            //given
            var store = new StringStore();

            var writeFunc = Funcs.WriteValue(store);
            var appendFunc = Funcs.AppendValue(store);

            var sut = new DelegatesDependendClass(writeFunc, appendFunc);

            //when
            var text = sut.AddText("first", "second");

            //then
            text.Should().Be("first second");
        }

        [Test]
        public void can_resolve_class_with_injected_delegates()
        {
            //given
            var builder = new ContainerBuilder();

            builder.RegisterType<StringStore>().SingleInstance();
            builder.Register(ctx => Funcs.WriteValue(ctx.Resolve<StringStore>()));
            builder.Register(ctx => Funcs.AppendValue(ctx.Resolve<StringStore>()));
            builder.RegisterType<DelegatesDependendClass>();

            var container = builder.Build();

            var sut = container.Resolve<DelegatesDependendClass>();

            //when

            var text = sut.AddText("first", "second");

            //then
            text.Should().Be("first second");
        }

        [Test]
        public void can_resolve_delegate_with_injected_delegates()
        {
            //given
            var builder = new ContainerBuilder();

            builder.RegisterType<StringStore>().SingleInstance();
            builder.Register(ctx => Funcs.WriteValue(ctx.Resolve<StringStore>()));
            builder.Register(ctx => Funcs.AppendValue(ctx.Resolve<StringStore>()));
            builder.Register(ctx => Funcs.ConcatValues(
                ctx.Resolve<WriteValue>(),
                ctx.Resolve<AppendValue>()));

            var container = builder.Build();

            var sut = container.Resolve<ConcatValues>();

            //when

            var text = sut("first", "second");

            //then
            text.Should().Be("first second");
        }

        [Test]
        public void can_resolve_delegate_with_injected_delegates_when_registered_with_static_funcs()
        {
            //given
            var builder = new ContainerBuilder();

            builder.RegisterType<StringStore>().SingleInstance();
            builder.RegisterDelegate(Funcs.WriteValue);
            builder.RegisterDelegate(Funcs.AppendValue);
            builder.RegisterDelegate(Funcs.ConcatValues);

            var container = builder.Build();

            var sut = container.Resolve<ConcatValues>();

            //when

            var text = sut("first", "second");

            //then
            text.Should().Be("first second");
        }

        [Test]
        public void can_resolve_delegate_with_injected_delegates_when_registerd_with_anonymous_functions()
        {
            //given
            var builder = new ContainerBuilder();

            builder.RegisterType<StringStore>().SingleInstance();

            builder.RegisterDelegate<StringStore, WriteValue>(store => arg =>
            {
                store.Value = arg;
            });

            builder.RegisterDelegate<StringStore, AppendValue>(store => arg => $"{store.Value} {arg}");

            builder.RegisterDelegate<WriteValue, AppendValue, ConcatValues>((write, append) => (arg1, arg2) =>
            {
                write(arg1);
                return append(arg2);
            });


            var container = builder.Build();

            var sut = container.Resolve<ConcatValues>();

            //when

            var text = sut("first", "second");

            //then
            text.Should().Be("first second");
        }

        [Test]
        public void can_resolve_delegate_with_injected_delegates_when_registerd_with_named_functions()
        {
            //given
            var builder = new ContainerBuilder();
            
            builder.RegisterType<StringStore>().SingleInstance();

            builder.RegisterDelegate<StringStore, WriteValue>(
                store => arg => Funcs.Write(arg, store));

            builder.RegisterDelegate<StringStore, AppendValue>(
                store => arg => Funcs.Append(arg, store));

            builder.RegisterDelegate<WriteValue, AppendValue, ConcatValues>(
                (write, append) => (arg1, arg2) => Funcs.Concat(arg1, arg2, write, append));


            var container = builder.Build();

            var sut = container.Resolve<ConcatValues>();

            //when

            var text = sut("first", "second");

            //then
            text.Should().Be("first second");
        }
    }
}
