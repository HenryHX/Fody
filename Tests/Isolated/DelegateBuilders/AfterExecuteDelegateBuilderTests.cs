using System;
using NUnit.Framework;

[TestFixture]
public class AfterExecuteDelegateBuilderTests
{
    [Test]
    public void Should_not_throw_When_no_method()
    {
        typeof(NoExecuteClass).BuildAfterWeavingDelegate();
    }

    public class NoExecuteClass
    {
    }

    [Test]
    public void Find_and_run()
    {
        var action = typeof(ValidClass).BuildAfterWeavingDelegate();
        action(new ValidClass());
    }

    [Test]
    public void Find_and_run_from_interface()
    {
        var action = typeof(WeaverFromBase).BuildAfterWeavingDelegate();
        action(new WeaverFromBase());
    }

    public class ValidClass
    {
        public void AfterWeaving()
        {
        }
    }

    [Test]
    public void Should_not_throw_When_method_is_not_public()
    {
        typeof(NonPublicClass).BuildAfterWeavingDelegate();
    }

    public class NonPublicClass
    {
// ReSharper disable UnusedMember.Local
        void AfterWeavingExecute()
// ReSharper restore UnusedMember.Local
        {
        }
    }

    [Test]
    public void Should_not_throw_When_method_is_static()
    {
        typeof(StaticExecuteClass).BuildAfterWeavingDelegate();
    }

    public class StaticExecuteClass
    {
        public static void AfterWeaving()
        {
        }
    }

    [Test]
    public void Should_thrown_inner_exception_When_delegate_is_executed()
    {
        var action = typeof (ThrowFromExecuteClass).BuildAfterWeavingDelegate();
        Assert.Throws<NullReferenceException>(() => action(new ThrowFromExecuteClass()));
    }

    public class ThrowFromExecuteClass
    {
        public void AfterWeaving()
        {
            throw new NullReferenceException();
        }
    }
}