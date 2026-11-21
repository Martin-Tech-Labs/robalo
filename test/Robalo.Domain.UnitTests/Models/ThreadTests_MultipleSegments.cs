using System.Reflection.Metadata;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories.Sqlite.Dto;
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
        var events = new List<MessageAdded>();

        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _utcNow.AddSeconds(3));
        var context = thread.Context.ToList();
        events.AddRange(thread.PendingEvents);
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

        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBe(thread.IsAnswered);
        thread2.Context.ShouldBeEquivalentTo(thread.Context);
        thread2.ExtendedContext.ShouldBeEmpty();
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
        thread.Context[0].OriginalMessageId.ShouldBe(thread.PendingEvents[1].MessageId);
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(context);

        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(5));

        thread.MarkAggregateAsSynchronized();
        thread.ExtendedContext.ShouldBeEmpty();
    }

    [Fact]
    public void AddBreak_With_Summary_And_Other_Messages()
    {
        var events = new List<MessageAdded>();
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _utcNow.AddSeconds(3));
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(4));

        thread.AddSummaryMessage(_fixture.Create<string>(), thread.ExtendedContext.Select(c => c.MessageId).ToList(), _utcNow.AddSeconds(5));
        thread.AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(6));
        thread.AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(7));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[1].MessageId, _utcNow.AddSeconds(8));
        thread.IsAnswered.ShouldBeFalse();
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[^1].MessageId, _utcNow.AddSeconds(9));
        thread.IsAnswered.ShouldBeTrue();
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(9));

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.ShouldBeEquivalentTo(thread.Context);
        thread2.ExtendedContext.ShouldBeEmpty();
        thread2.ModifiedOn.ShouldBe(thread.ModifiedOn);
        thread2.PersistedVersion.ValueOrFailure.ShouldBe(events.Count);
    }

    [Fact]
    public void AddBreak_With_Summary_And_Other_Messages_MultipleTimes()
    {
        var events = new List<MessageAdded>();
        var thread = Thread.NewThread(_utcNow);

        for (var i = 0; i < 10; i++)
        {
            thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
            thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
            thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.ModifiedOn.AddSeconds(1));
            events.AddRange(thread.PendingEvents);
            thread.MarkAggregateAsSynchronized();
            thread.IsAnswered.ShouldBeFalse();
            thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
            thread.IsAnswered.ShouldBeTrue();
            thread.AddSummaryMessage(_fixture.Create<string>(), thread.ExtendedContext.Select(c => c.MessageId).ToList(), thread.ModifiedOn.AddSeconds(1));
            events.AddRange(thread.PendingEvents);
            thread.MarkAggregateAsSynchronized();
            thread.IsAnswered.ShouldBeTrue();
        }

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.ShouldBeEquivalentTo(thread.Context);
        thread2.ExtendedContext.ShouldBeEmpty();
        thread2.ModifiedOn.ShouldBe(thread.ModifiedOn);
    }


    [Fact]
    public void AddCopyOfMessage_ShouldThrow_OutsideOfBreakPhrase()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);
        var message = thread.ExtendedContext[0];
        thread.MarkAggregateAsSynchronized();

        Should.Throw<InvalidOperationException>(() => thread.AddCopyOfMessage(message, _utcNow));
    }

    [Fact]
    public void AddCopyOfMessage_ShouldThrow_OnInvalidAddedOn()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);
        var message = thread.ExtendedContext[0];

        Should.Throw<ArgumentOutOfRangeException>(() => thread.AddCopyOfMessage(message, _utcNow.AddSeconds(-1)));
    }

    [Fact]
    public void AddCopyOfMessage_ShouldThrow_OnInvalidMessage()
    {
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow);

        Should.Throw<ArgumentOutOfRangeException>(() => thread.AddCopyOfMessage(_fixture.Create<ContextMessage>(), _utcNow.AddSeconds(-1)));
    }

    [Fact]
    public void AddCopyOfMessage_ShouldCopy_UserMessage()
    {
        var events = new List<MessageAdded>();
        var content = _fixture.Create<string>();
        var thread = Thread.NewThread(_utcNow).AddUserMessage(content, _utcNow);
        events.AddRange(thread.PendingEvents);
        var originalMessageId = thread.PendingEvents[^1].MessageId;
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[0], _utcNow.AddSeconds(2));
        thread.Context.Count.ShouldBe(1);
        var messageId = thread.PendingEvents[^1].MessageId;
        thread.Context[0].MessageId.ShouldNotBe(thread.Context[0].OriginalMessageId);
        thread.Context[0].OriginalMessageId.ShouldBe(originalMessageId);
        thread.Context[0].Content.ShouldBe(thread.ExtendedContext[0].Content);
        thread.Context[0].Role.ShouldBe(MessageRole.User);
        thread.PendingEvents[1].AddedOn.ShouldBe(_utcNow.AddSeconds(2));
        thread.PendingEvents[1].Content.ShouldBe(content);
        thread.PendingEvents[1].MessageId.ShouldBe(thread.Context[0].MessageId);
        thread.PendingEvents[1].ShouldBeOfType<UserMessageCopied>();
        var messageCopied = (UserMessageCopied)thread.PendingEvents[1];
        messageCopied.OriginalMessageId.ShouldBe(originalMessageId);
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.IsAnswered.ShouldBeFalse();
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(2));
        var context = thread.Context;

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBeFalse();
        thread2.Context.ShouldBeEquivalentTo(context);
        thread2.ModifiedOn.ShouldBe(thread.ModifiedOn);

        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeTrue();
        thread2.AddCopyOfMessage(thread2.ExtendedContext.Single(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeFalse();
        thread2.Context.Single().Role.ShouldBe(MessageRole.User);
        thread2.Context.Single().Content.ShouldBe(content);
        thread2.Context.Single().MessageId.ShouldNotBe(messageId);
        thread2.Context.Single().OriginalMessageId.ShouldBe(originalMessageId);
    }

    [Fact]
    public void AddCopyOfMessage_ShouldCopy_AssistantMessage()
    {
        var events = new List<MessageAdded>();
        var content = _fixture.Create<string>();
        var thread = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        var repliedToMessageId = thread.Context.Single().MessageId;
        thread.AddAssistantMessage(content, repliedToMessageId, thread.ModifiedOn.AddSeconds(1));
        events.AddRange(thread.PendingEvents);
        var originalMessageId = thread.PendingEvents[^1].MessageId;
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));
        thread.AddCopyOfMessage(thread.ExtendedContext[1], _utcNow.AddSeconds(3));
        thread.Context.Count.ShouldBe(1);
        var messageId = thread.PendingEvents[^1].MessageId;
        thread.Context[0].MessageId.ShouldNotBe(originalMessageId);
        thread.Context[0].OriginalMessageId.ShouldBe(originalMessageId);
        thread.Context[0].Content.ShouldBe(content);
        thread.Context[0].Role.ShouldBe(MessageRole.Assistant);
        thread.Context[0].RepliedToMessageId.ShouldBe(repliedToMessageId);

        thread.PendingEvents[1].AddedOn.ShouldBe(_utcNow.AddSeconds(3));
        thread.PendingEvents[1].Content.ShouldBe(content);
        thread.PendingEvents[1].MessageId.ShouldBe(thread.Context[0].MessageId);
        thread.PendingEvents[1].ShouldBeOfType<AssistantMessageCopied>();
        var messageCopied = (AssistantMessageCopied)thread.PendingEvents[1];
        messageCopied.OriginalMessageId.ShouldBe(originalMessageId);
        messageCopied.RepliedToMessageId.ShouldBe(repliedToMessageId);
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.IsAnswered.ShouldBeTrue();
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(3));
        var context = thread.Context;

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.ShouldBeEquivalentTo(context);
        thread2.ModifiedOn.ShouldBe(thread.ModifiedOn);

        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeTrue();
        thread2.AddCopyOfMessage(thread2.ExtendedContext.Single(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.Single().Role.ShouldBe(MessageRole.Assistant);
        thread2.Context.Single().Content.ShouldBe(content);
        thread2.Context.Single().MessageId.ShouldNotBe(messageId);
        thread2.Context.Single().OriginalMessageId.ShouldBe(originalMessageId);
        thread2.Context.Single().RepliedToMessageId.ShouldBe(repliedToMessageId);
    }

    [Fact]
    public void AddCopyOfSummary_ShouldCopy_SummaryMessage()
    {
        var events = new List<MessageAdded>();
        var content = _fixture.Create<string>();
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(3));
        var references = thread.Context.Select(c => c.MessageId).ToList();
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(4));
        thread.AddSummaryMessage(content, references, _utcNow.AddSeconds(5));
        var originalMessageId = thread.PendingEvents[^1].MessageId;
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(6));
        thread.AddCopyOfMessage(thread.ExtendedContext.Single(), _utcNow.AddSeconds(7));
        thread.Context.Count.ShouldBe(1);
        var messageId = thread.PendingEvents[^1].MessageId;
        thread.Context[0].MessageId.ShouldNotBe(originalMessageId);
        thread.Context[0].OriginalMessageId.ShouldBe(originalMessageId);
        thread.Context[0].Content.ShouldBe(content);
        thread.Context[0].Role.ShouldBe(MessageRole.Summary);
        thread.Context[0].References.ValueOrFailure.ToList().ShouldBeEquivalentTo(references);

        thread.PendingEvents[1].AddedOn.ShouldBe(_utcNow.AddSeconds(7));
        thread.PendingEvents[1].Content.ShouldBe(content);
        thread.PendingEvents[1].MessageId.ShouldBe(thread.Context[0].MessageId);
        thread.PendingEvents[1].ShouldBeOfType<SummaryMessageCopied>();
        var messageCopied = (SummaryMessageCopied)thread.PendingEvents[1];
        messageCopied.OriginalMessageId.ShouldBe(originalMessageId);
        messageCopied.References.ToList().ShouldBeEquivalentTo(references);
        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.IsAnswered.ShouldBeTrue();
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(7));
        var context = thread.Context;

        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.ShouldBeEquivalentTo(context);
        thread2.ModifiedOn.ShouldBe(thread.ModifiedOn);

        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeTrue();
        thread2.AddCopyOfMessage(thread2.ExtendedContext.Single(), thread2.ModifiedOn.AddSeconds(1));
        thread2.IsAnswered.ShouldBeTrue();
        thread2.Context.Single().Role.ShouldBe(MessageRole.Summary);
        thread2.Context.Single().Content.ShouldBe(content);
        thread2.Context.Single().MessageId.ShouldNotBe(messageId);
        thread2.Context.Single().OriginalMessageId.ShouldBe(originalMessageId);
        thread2.Context.Single().References.ValueOrFailure.ToList().ShouldBe(references);
    }

    [Fact]
    public void AddAssistantMessage_CanReply_To_MessageInExtendedContext()
    {
        var events = new List<MessageAdded>();
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(3));

        var messageId = thread.Context[^1].MessageId;

        events.AddRange(thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        thread.AddBreakMessage(_fixture.Create<string>(), _utcNow.AddSeconds(4));
        thread.AddAssistantMessage(_fixture.Create<string>(), messageId, _utcNow.AddSeconds(5));

        thread.Context[^1].RepliedToMessageId.ShouldBe(messageId);
        (thread.PendingEvents[^1] as AssistantMessageAdded)!.RepliedToMessageId.ShouldBe(messageId);
        events.AddRange(thread.PendingEvents);

        thread.MarkAggregateAsSynchronized();
        var thread2 = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PersistedVersion.ValueOrFailure, events);
        thread2.Context.ShouldBeEquivalentTo(thread.Context);
    }
}