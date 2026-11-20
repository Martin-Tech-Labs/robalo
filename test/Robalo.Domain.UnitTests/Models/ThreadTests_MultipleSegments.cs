using System.Reflection.Metadata;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Tests.Common;

namespace Robalo.Domain.UnitTests.Models;

public class ThreadTests_MultipleSegments
{
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;

    public ThreadTests_MultipleSegments()
    {
        _utcNow = _fixture.Create<DateTimeOffset>();
    }

    [Fact]
    public void AddBreakMessage_ShouldThrow_OnEmptyThread()
    {
        var thread = Thread.NewThread(_utcNow);
        thread.MarkAggregateAsSynchronized();
        Should.Throw<InvalidOperationException>(() => thread.AddBreakMessage(_fixture.Create<string>(), _utcNow));
    }

    [Fact]
    public void AddBreakMessage_ShouldThrow_OnNonEmptyUnsavedThread()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        Should.Throw<InvalidOperationException>(() => thread.AddBreakMessage(_fixture.Create<string>(), _utcNow));
    }

    [Fact]
    public void AddBreakMessage_ShouldThrow_OnWrongAddedOn()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        Should.Throw<ArgumentOutOfRangeException>(() => thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(-1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddBreakMessage_ShouldThrow_OnWrongContent(string content)
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        Should.Throw<ArgumentException>(() => thread.AddBreakMessage(content, _utcNow.AddSeconds(1)));
    }

    [Fact]
    public void AddBreakMessage_ShouldThrowOnPendingBreak()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);
        thread.AddUserMessage(_fixture.Create<string>(), _utcNow);
        Should.Throw<InvalidOperationException>(() => thread.AddBreakMessage(_fixture.Create<string>(), _utcNow));
    }

    [Fact]
    public void AddBreakMessage_ShouldThrow_OnEmptySegment()
    {
        var thread = Thread.Rehydrate(ThreadIdentifier.NewIdentifier(), _utcNow, null, 2,
        [new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow),
        new BreakMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow)]);

        thread.ExtendedContext.ShouldBeEmpty();
        Should.Throw<InvalidOperationException>(() => thread.AddBreakMessage(_fixture.Create<string>(), _utcNow));
    }

    [Fact]
    public void AddBreakMessage_ShouldWorkAsExpected()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _utcNow.AddSeconds(3));
        var context = thread.Context.ToList();
        thread.MarkAggregateAsSynchronized();
        thread.IsAnswered.ShouldBeFalse();
        var content = _fixture.Create<string>();
        thread.AddBreakMessage(content, _utcNow.AddSeconds(4));
        thread.PendingVersion.ShouldBe(4);
        thread.PendingEvents.Count.ShouldBe(1);
        thread.PendingEvents[0].ShouldBeOfType<BreakMessageAdded>();
        thread.PendingEvents[0].Content.ShouldBe(content);
        thread.PendingEvents[0].AddedOn.ShouldBe(_utcNow.AddSeconds(4));
        thread.IsAnswered.ShouldBeTrue();
        thread.Context.ShouldBeEmpty();
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(context);

        thread.MarkAggregateAsSynchronized();
        thread.ExtendedContext.ShouldBeEmpty();
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OnNoSegments()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();

        Should.Throw<InvalidOperationException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { thread.Context[0].MessageId }, _utcNow));
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OutsideOfBreakPhrase()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);
        var messageId = thread.ExtendedContext[0].MessageId;
        thread.MarkAggregateAsSynchronized();

        Should.Throw<InvalidOperationException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { messageId }, _utcNow));
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OnInvalidId()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow);

        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);

        Should.Throw<ArgumentException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { _fixture.Create<MessageIdentifier>(), thread.ExtendedContext[0].MessageId }, _utcNow));
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OnEmptyReferences()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow);

        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);

        Should.Throw<ArgumentException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { }, _utcNow));
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OnDuplicateIds()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow);

        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);

        Should.Throw<ArgumentException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { thread.ExtendedContext[0].MessageId, thread.ExtendedContext[0].MessageId }, _utcNow));
    }

    [Fact]
    public void AddSummaryMessage_ShouldThrow_OnInvalidAddedOn()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(3));

        Should.Throw<ArgumentOutOfRangeException>(() => thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { thread.ExtendedContext[0].MessageId }, _utcNow));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void AddSummaryMessage_ShouldThrow_OnInvalidContent(string content)
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(3));

        Should.Throw<ArgumentException>(() => thread.AddSummaryMessage(content, new List<MessageIdentifier> { thread.ExtendedContext[0].MessageId }, _utcNow.AddSeconds(4)));
    }

    [Fact]
    public void AddSummaryMessage_ShouldWorkAsExpected()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _utcNow.AddSeconds(3));
        var context = thread.Context.ToList();
        thread.MarkAggregateAsSynchronized();
        thread.IsAnswered.ShouldBeFalse();
        var breakContent = _fixture.Create<string>();
        var sumamaryContent = _fixture.Create<string>();
        thread.AddBreakMessage(breakContent, _utcNow.AddSeconds(4));
        thread.Context.ShouldBeEmpty();
        thread.AddSummaryMessage(sumamaryContent, thread.ExtendedContext.Select(c => c.MessageId).ToList(), _utcNow.AddSeconds(5));

        thread.PendingVersion.ShouldBe(5);
        thread.PendingEvents.Count.ShouldBe(2);
        thread.PendingEvents[0].ShouldBeOfType<BreakMessageAdded>();
        thread.PendingEvents[0].Content.ShouldBe(breakContent);
        thread.PendingEvents[0].AddedOn.ShouldBe(_utcNow.AddSeconds(4));
        thread.PendingEvents[1].ShouldBeOfType<SummaryMessageAdded>();
        thread.PendingEvents[1].Content.ShouldBe(sumamaryContent);
        thread.PendingEvents[1].AddedOn.ShouldBe(_utcNow.AddSeconds(5));
        ((SummaryMessageAdded)thread.PendingEvents[1]).References.ToArray().ShouldBeEquivalentTo(thread.ExtendedContext.Select(c => c.MessageId).ToArray());

        thread.IsAnswered.ShouldBeTrue();
        thread.Context.Count.ShouldBe(1);
        thread.Context[0].Content.ShouldBe(sumamaryContent);
        thread.Context[0].Role.ShouldBe(MessageRole.Summary);
        thread.Context[0].References.HasValue.ShouldBeTrue();
        thread.Context[0].References.ValueOrFailure.ToList().ShouldBeEquivalentTo(thread.ExtendedContext.Select(c => c.MessageId).ToList());
        thread.Context[0].MessageId.ShouldBe(thread.PendingEvents[1].MessageId);
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(context);

        thread.MarkAggregateAsSynchronized();
        thread.ExtendedContext.ShouldBeEmpty();
    }
}