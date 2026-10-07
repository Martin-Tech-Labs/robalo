using Robalo.Common.Models;
using Shouldly;

namespace Robalo.Tests.Common;

public static class OptionAssertions
{
    public static void ShouldBe<T>(
        this T actual,
        Option<T> expected) where T : notnull
    {
        (actual == expected).ShouldBeTrue(
            $"Expected {actual} to equal {expected}.");
    }

    public static void ShouldBe<T>(
        this Option<T> actual,
        T expected) where T : notnull
    {
        (actual == expected).ShouldBeTrue(
            $"Expected {actual} to equal {expected}.");
    }

    public static void ShouldNotBe<T>(this T actual, Option<T> expected) where T : notnull
    {
        (actual != expected).ShouldBeTrue(
            $"Expected {actual} to differ from {expected}.");
    }

    public static void ShouldNotBe<T>(this Option<T> actual, T expected) where T : notnull
    {
        (actual != expected).ShouldBeTrue(
            $"Expected {actual} to differ from {expected}.");
    }

    public static void ShouldBeNone<T>(this Option<T> value) where T : notnull
    {
        (value is None).ShouldBeTrue($"Expected {value} to be None");
    }
}