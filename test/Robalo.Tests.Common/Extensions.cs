using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Shouldly;

namespace Robalo.Tests.Common;

public static class Extensions
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldBeEquivalentToWithIgnoreMessageId(
        this IEnumerable<Message> actual,
        IEnumerable<Message> expected,
        string? customMessage = null)
    {
        var messageId = MessageIdentifier.NewIdentifier();
        var newExpected = expected.Select(m => m with { MessageId = messageId }).ToList();
        var newActual = actual.Select(m => m with { MessageId = messageId }).ToList();
        newActual.ShouldBeEquivalentTo(newExpected);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void ShouldBeEquivalentToWithIgnoreMessageId(
        this Message actual,
        Message expected,
        string? customMessage = null)
    {
     
        expected.ShouldBe(actual with {MessageId = expected.MessageId});
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