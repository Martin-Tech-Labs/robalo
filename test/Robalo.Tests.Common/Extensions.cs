using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Shouldly;

namespace Robalo.Tests.Common;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldBeEquivalentToWithGivenMessageIds(
        this IEnumerable<Message> actual,
        IEnumerable<Message> expected,
        IReadOnlyList<MessageIdentifier> messageIds,
        string? customMessage = null)
    {

        var newExpected = expected.ToList();
        for (var i = 0; i < newExpected.Count; i++)
        {
            newExpected[i] = newExpected[i] with { MessageId = messageIds[newExpected[i].Number - 1] };
        }

        actual.ToList().ShouldBeEquivalentTo(newExpected);
    }

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