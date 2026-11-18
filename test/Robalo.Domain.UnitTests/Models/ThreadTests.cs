using System.Reflection.Metadata;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Tests.Common;

namespace Robalo.Domain.UnitTests.Models;

public class ThreadTests
{
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;

    public ThreadTests()
    {
        _utcNow = _fixture.Create<DateTimeOffset>();
    }

    [Fact]
    public void NewThread_ShouldThrowOnInvalidDate()
    {
        Should.Throw<ArgumentException>(() => Thread.NewThread(default));
    }

    [Fact]
    public void NewThread_ShouldCreateThread()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();

        var thread = Thread.NewThread(createdOn);
        thread.CreatedOn.ShouldBe(createdOn);
        thread.ModifiedOn.ShouldBe(createdOn);
        thread.Id.ShouldNotBeNull();
        thread.Context.ShouldBeEmpty();
        thread.PendingEvents.ShouldBeEmpty();
        thread.PendingVersion.ShouldBe(0);
        thread.PersistedVersion.ShouldBeNone();

        thread.IsAnswered.ShouldBeTrue();
    }

    [Fact]
    public void AddUserMessage_ShouldThrowOnWrongDate()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        Should.Throw<ArgumentException>(() => thread.AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(-1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]

    public void AddUserMessage_ShouldThrowOnEmptyMessage(string messageContent)
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        Should.Throw<ArgumentException>(() => thread.AddUserMessage(messageContent, createdOn.AddSeconds(1)));
    }

    [Fact]
    public void AddUserMessage_ShouldAddMessage()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        var messageContent = _fixture.Create<string>();
        var addedOn = createdOn.AddSeconds(1);

        thread.AddUserMessage(messageContent, addedOn);

        thread.Context.Count.ShouldBe(1);
        thread.PendingEvents.Count.ShouldBe(1);

        thread.PendingEvents.Single().ShouldBeOfType<UserMessageAdded>();
        var @event = (UserMessageAdded)thread.PendingEvents.Single();

        @event.AddedOn.ShouldBe(addedOn);
        @event.Content.ShouldBe(messageContent);

        thread.Context[0].Content.ShouldBe(messageContent);
        thread.Context[0].Source.ShouldBe(MessageRole.User);
        thread.Context[0].RepliedToMessageId.ShouldBeNone();
        thread.Context[0].MessageId.ShouldBe(thread.PendingEvents[0].MessageId);

        thread.PendingVersion.ShouldBe(1);
        thread.PersistedVersion.ShouldBeNone();

        thread.ModifiedOn.ShouldBe(addedOn);

        thread.IsAnswered.ShouldBeFalse();
    }

    [Fact]
    public void AddSecondUserMessage_ShouldFailOnInvalidDate()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        thread.AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(1));
        Should.Throw<ArgumentException>(() => thread.AddUserMessage(_fixture.Create<string>(), createdOn));
    }

    [Fact]
    public void AddTwoUserMessages_ShouldAddMessages()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);
        var oldVersion = thread.PersistedVersion;

        thread.AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(2));

        thread.Context.Count.ShouldBe(2);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(2);
        thread.Context[0].MessageId.ShouldBe(thread.PendingEvents[0].MessageId);
        thread.Context[1].MessageId.ShouldBe(thread.PendingEvents[1].MessageId);

        thread.PendingVersion.ShouldBe(2);
        thread.PersistedVersion.ShouldBeNone();

        thread.ModifiedOn.ShouldBe(createdOn.AddSeconds(2));

        thread.PendingEvents.Count.ShouldBe(2);

        var @event1 = (UserMessageAdded)thread.PendingEvents[0];
        var @event2 = (UserMessageAdded)thread.PendingEvents[1];

        @event1.Content.ShouldBe(thread.Context[0].Content);
        @event2.Content.ShouldBe(thread.Context[1].Content);
    }

    [Fact]
    public void AddLlmMessage_ShouldThrowIfNoMessages()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        Should.Throw<InvalidOperationException>(() => thread.AddLlmMessage(_fixture.Create<string>(), MessageIdentifier.NewIdentifier(), thread.CreatedOn));
    }

    [Fact]
    public void CreateLlmMessageAdded_ShouldThrowIfNoMessages()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        Should.Throw<InvalidOperationException>(() => thread.CreateLlmMessageAdded(_fixture.Create<string>(), MessageIdentifier.NewIdentifier(), thread.CreatedOn));
    }



    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddLlmMessage_ShouldThrowOnEmptyContent(string content)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(content, thread.Context[0].MessageId, thread.CreatedOn));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void CreateLlmMessage_ShouldThrowOnEmptyContent(string content)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.CreateLlmMessageAdded(content, thread.Context[0].MessageId, thread.CreatedOn));
    }

    [Fact]
    public void AddLlmMessage_ShouldThrowOnInvalidDate()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.CreatedOn.AddSeconds(-5)));
    }

    [Fact]
    public void CreateLlmMessage_ShouldThrowOnInvalidDate()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.CreateLlmMessageAdded(_fixture.Create<string>(), thread.Context[0].MessageId, thread.CreatedOn.AddSeconds(-5)));
    }

    [Fact]
    public void AddLlmMessage_ShouldThrowOnInvalidRepliedToMessage()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(_fixture.Create<string>(), MessageIdentifier.NewIdentifier(), thread.CreatedOn));
    }

    [Fact]
    public void CreateLlmMessage_ShouldThrowOnInvalidRepliedToMessage()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.CreateLlmMessageAdded(_fixture.Create<string>(), MessageIdentifier.NewIdentifier(), thread.CreatedOn));
    }

    [Fact]
    public void CreateLlmMessage_ShouldReturnEvent_And_Not_Change_Thread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.MarkAggregateAsSynchronized();

        var content = _fixture.Create<string>();
        var addedOn = thread.CreatedOn.AddSeconds(5);

        var result = thread.CreateLlmMessageAdded(content, thread.Context[^1].MessageId, addedOn);
        result.AddedOn.ShouldBe(addedOn);
        result.Content.ShouldBe(content);
        thread.PersistedVersion.ShouldBe(thread.PendingVersion);
        thread.PendingEvents.Count.ShouldBe(0);
    }

    [Fact]
    public void AddLlmMessage_ForSameUserMessage_ShouldCorrectlyUpdateContext()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        var u1 = thread.PendingEvents[^1].MessageId;
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        var u2 = thread.PendingEvents[^1].MessageId;
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        var u3 = thread.PendingEvents[^1].MessageId;
        thread.AddLlmMessage(_fixture.Create<string>(), u1, thread.CreatedOn);
        var l1_u1 = thread.PendingEvents[^1].MessageId;
        thread.AddLlmMessage(_fixture.Create<string>(), u1, thread.CreatedOn);
        var l2_u1 = thread.PendingEvents[^1].MessageId;
        thread.AddLlmMessage(_fixture.Create<string>(), u1, thread.CreatedOn);
        var l3_u1 = thread.PendingEvents[^1].MessageId;
        thread.AddLlmMessage(_fixture.Create<string>(), l2_u1, thread.CreatedOn);
        var l1_l2 = thread.PendingEvents[^1].MessageId;
        thread.AddLlmMessage(_fixture.Create<string>(), l2_u1, thread.CreatedOn);
        var l2_l2 = thread.PendingEvents[^1].MessageId;

        thread.Context.Select(c => c.MessageId).ShouldBe(new[] { u1, l1_u1, l2_u1, l1_l2, l2_l2, l3_u1, u2, u3 }.AsEnumerable());
        thread.IsAnswered.ShouldBeFalse();

        thread.AddLlmMessage(_fixture.Create<string>(), u3, thread.CreatedOn);
        var l1_u3 = thread.PendingEvents[^1].MessageId;
        thread.IsAnswered.ShouldBeTrue();
        thread.Context.Select(c => c.MessageId).ShouldBe(new[] { u1, l1_u1, l2_u1, l1_l2, l2_l2, l3_u1, u2, u3, l1_u3 }.AsEnumerable());

        thread.AddLlmMessage(_fixture.Create<string>(), u3, thread.CreatedOn);
        var l2_u3 = thread.PendingEvents[^1].MessageId;

        thread.AddLlmMessage(_fixture.Create<string>(), u2, thread.CreatedOn);
        var l1_u2 = thread.PendingEvents[^1].MessageId;

        thread.AddLlmMessage(_fixture.Create<string>(), l1_u3, thread.CreatedOn);
        var l1_l1_u3 = thread.PendingEvents[^1].MessageId;

        thread.IsAnswered.ShouldBeTrue();
        thread.Context.Select(c => c.MessageId).ShouldBe(new[] { u1, l1_u1, l2_u1, l1_l2, l2_l2, l3_u1, u2, l1_u2, u3, l1_u3, l1_l1_u3, l2_u3 }.AsEnumerable());

        thread.PendingEvents.Select(c => c.MessageId).ShouldBe(new[]
        {
            u1,u2,u3,l1_u1,l2_u1,l3_u1,l1_l2,l2_l2,l1_u3,l2_u3,l1_u2,l1_l1_u3
        }.AsEnumerable());


        var threadRehydrated = Thread.Rehydrate(thread.Id, thread.CreatedOn, thread.Title, thread.PendingVersion, thread.PendingEvents);
        thread.MarkAggregateAsSynchronized();
        threadRehydrated.Context.ShouldBeEquivalentTo(thread.Context);
        threadRehydrated.PendingVersion.ShouldBe(thread.PendingVersion);
        threadRehydrated.PersistedVersion.ShouldBe(thread.PersistedVersion);
    }

    [Theory]
    [InlineData(0, false, 1)]
    [InlineData(1, true, 2)]
    public void AddLlmMessage_ShouldUpdateContextCorrectly_And_Set_Answered_Correctly(int repliedToMessageIndex, bool isAnswered, int indexLLmMessage)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        var content1 = _fixture.Create<string>();
        var content2 = _fixture.Create<string>();
        thread.AddUserMessage(content1, thread.CreatedOn);
        thread.AddUserMessage(content2, thread.CreatedOn);

        var versionBeforeLlmMessage = thread.PendingVersion;
        var content = _fixture.Create<string>();
        var addedOn = thread.CreatedOn.AddSeconds(1);

        thread.AddLlmMessage(content, thread.Context[repliedToMessageIndex].MessageId, addedOn);

        thread.IsAnswered.ShouldBe(isAnswered);
        thread.Context.Count.ShouldBe(3);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(3);

        thread.PendingVersion.ShouldNotBe(versionBeforeLlmMessage);
        thread.ModifiedOn.ShouldBe(thread.CreatedOn.AddSeconds(1));

        thread.PendingEvents.Count.ShouldBe(3);
        thread.PendingEvents[2].ShouldBeOfType<AssistantMessageAdded>();

        var event3 = (AssistantMessageAdded)thread.PendingEvents[2];
        event3.AddedOn.ShouldBe(addedOn);
        event3.Content.ShouldBe(content);
        event3.RepliedToMessageId.ShouldBe(thread.Context[repliedToMessageIndex].MessageId);

        thread.Context.Count.ShouldBe(3);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(3);
        thread.Context[indexLLmMessage].Content.ShouldBe(content);
        thread.Context[indexLLmMessage].Source.ShouldBe(MessageRole.LLM);
        thread.Context[indexLLmMessage].RepliedToMessageId.ShouldBe(thread.Context[repliedToMessageIndex].MessageId);
        thread.Context[indexLLmMessage].MessageId.ShouldBe(thread.PendingEvents[2].MessageId);

        thread.Context.First(f => f.Source == MessageRole.User).Content.ShouldBe(content1);
        thread.Context.Last(f => f.Source == MessageRole.User).Content.ShouldBe(content2);
    }

    [Fact]
    public void AddUserMessage_ShouldSetAnsweredToFalse_IfWasTrue()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.IsAnswered.ShouldBeFalse();

        thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.CreatedOn);
        thread.IsAnswered.ShouldBeTrue();

        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.IsAnswered.ShouldBeFalse();
    }

    [Fact]
    public void AddLlmMessage_ShouldNotAnsweredToFalse_IfWasTrue()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.IsAnswered.ShouldBeFalse();

        thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[1].MessageId, thread.CreatedOn);
        thread.IsAnswered.ShouldBeTrue();

        thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.CreatedOn);
        thread.IsAnswered.ShouldBeTrue();
    }

    [Fact]
    public void MarkAggregateAsSynchronized_ShouldCleanPendingEventsAndUpdateVersion()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        thread.AddUserMessage(_fixture.Create<string>(), createdOn);
        thread.AddUserMessage(_fixture.Create<string>(), createdOn);

        thread.MarkAggregateAsSynchronized();
        thread.PersistedVersion.ShouldBe(2);
        thread.PendingVersion.ShouldBe(2);

        thread.Context.Count.ShouldBe(2);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(2);

        thread.PendingEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAggregateAsSynchronized_ShouldThrow_IfCalledMultipleTimes()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(createdOn);

        thread.AddUserMessage(_fixture.Create<string>(), createdOn);
        thread.AddUserMessage(_fixture.Create<string>(), createdOn);

        thread.MarkAggregateAsSynchronized();

        Should.Throw<InvalidOperationException>(() => thread.MarkAggregateAsSynchronized());
    }

    [Fact]
    public void Rehydrate_ShouldThrow_IfVersionIsLessThan0()
    {
        Should.Throw<ArgumentOutOfRangeException>(() => Thread.Rehydrate(
            identifier: ThreadIdentifier.NewIdentifier(),
            createdOn: _fixture.Create<DateTimeOffset>(),
            title: _fixture.Create<string>(),
            persistedVersion: -1,
            events: []));
    }

    [Fact]
    public void Rehydrate_ShouldThrow_IfVersionIsLessNumberOfEvents()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        Should.Throw<ArgumentOutOfRangeException>(() => Thread.Rehydrate(
            identifier: ThreadIdentifier.NewIdentifier(),
            createdOn: _utcNow,
            title: _fixture.Create<string>(),
            persistedVersion: 1,
            events: [
                new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow),
                new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow)
                ]));
    }

    [Fact]
    public void Rehydrate_ShouldSucceed_OnNoEvents()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var title = _fixture.Create<string>();
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.Rehydrate(
            identifier: threadId,
            createdOn: createdOn,
            title: title,
            persistedVersion: 0,
            events: []);

        thread.Id.ShouldBe(threadId);
        thread.Title.ShouldBe(title);
        thread.CreatedOn.ShouldBe(createdOn);
        thread.ModifiedOn.ShouldBe(createdOn);

        thread.PendingEvents.ShouldBeEmpty();
        thread.Context.ShouldBeEmpty();

        thread.PersistedVersion.ShouldBe(0);
        thread.PendingVersion.ShouldBe(0);
    }

    [Fact]
    public void Rehydrate_ShouldSucceed_WithWrongRepliedToMessageOnLlmMessage()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var title = _fixture.Create<string>();

        List<MessageAdded> events = new();

        events.Add(new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow.AddSeconds(1)));
        events.Add(new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow.AddSeconds(2)));
        events.Add(new AssistantMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), events[0].MessageId, _utcNow.AddSeconds(3)));
        events.Add(new AssistantMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), MessageIdentifier.NewIdentifier(), _utcNow.AddSeconds(4)));

        var thread = Thread.Rehydrate(
            identifier: threadId,
            createdOn: _utcNow,
            title: title,
            persistedVersion: 10,
            events: events);

        thread.Id.ShouldBe(threadId);
        thread.Title.ShouldBe(title);
        thread.CreatedOn.ShouldBe(_utcNow);
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(4));

        thread.PendingEvents.ShouldBeEmpty();
        thread.Context.Count.ShouldBe(4);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(4);
        thread.Context.ToList().ShouldBeEquivalentTo(new List<MessageAdded> { events[0], events[2], events[1], events[3] }.Select(ContextMessage.FromModel).ToList());

        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(10);
        thread.IsAnswered.ShouldBeFalse();


        thread.AddLlmMessage(_fixture.Create<string>(), events[1].MessageId, _utcNow.AddSeconds(5));

        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(5));

        thread.PendingEvents.Count.ShouldBe(1);
        thread.Context.Count.ShouldBe(5);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(5);

        thread.Context.ToList().ShouldBeEquivalentTo(new List<MessageAdded> { events[0], events[2], events[1], thread.PendingEvents[^1], events[3] }.Select(ContextMessage.FromModel).ToList());


        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(11);

        thread.IsAnswered.ShouldBeTrue();

        thread.AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(6));
        thread.IsAnswered.ShouldBeFalse();
        thread.Context.ToList().ShouldBeEquivalentTo(new List<MessageAdded> { events[0], events[2], events[1], thread.PendingEvents[^2], events[3], thread.PendingEvents[^1] }.Select(ContextMessage.FromModel).ToList());

        thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[^1].MessageId, _utcNow.AddSeconds(7));
        thread.IsAnswered.ShouldBeTrue();
        thread.Context.ToList().ShouldBeEquivalentTo(new List<MessageAdded> { events[0], events[2], events[1], thread.PendingEvents[^3], events[3], thread.PendingEvents[^2], thread.PendingEvents[^1] }.Select(ContextMessage.FromModel).ToList());

    }

    [Fact]
    public void Rehydrate_ShouldSucceed_WithEvents()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var title = _fixture.Create<string>();

        List<MessageAdded> events = new();

        events.Add(new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow.AddSeconds(1)));
        events.Add(new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow.AddSeconds(2)));
        events.Add(new AssistantMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), events[1].MessageId, _utcNow.AddSeconds(3)));
        events.Add(new UserMessageAdded(MessageIdentifier.NewIdentifier(), _fixture.Create<string>(), _utcNow.AddSeconds(4)));

        var thread = Thread.Rehydrate(
            identifier: threadId,
            createdOn: _utcNow,
            title: title,
            persistedVersion: 10,
            events: events);

        thread.Id.ShouldBe(threadId);
        thread.Title.ShouldBe(title);
        thread.CreatedOn.ShouldBe(_utcNow);
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(4));

        thread.PendingEvents.ShouldBeEmpty();
        thread.Context.Count.ShouldBe(4);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(4);
        thread.Context.ToList().ShouldBeEquivalentTo(events.Select(ContextMessage.FromModel).ToList());


        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(10);
        thread.IsAnswered.ShouldBeFalse();


        thread.AddLlmMessage(_fixture.Create<string>(), thread.Context[3].MessageId, _utcNow.AddSeconds(5));

        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(5));

        thread.PendingEvents.Count.ShouldBe(1);
        thread.Context.Count.ShouldBe(5);
        thread.Context.Select(v => v.MessageId).Distinct().Count().ShouldBe(5);
        thread.Context[4].MessageId.ShouldBe(thread.PendingEvents[0].MessageId);


        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(11);

        thread.IsAnswered.ShouldBeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateUserMessageAdded_ThrowsOnInvalidContent(string content)
    {
        Should.Throw<ArgumentException>(() => Thread.CreateUserMessageAdded(content, _utcNow));
    }

    [Fact]
    public void CreateUserMessageAdded_ShouldCreateValidEvent()
    {
        var content = _fixture.Create<string>();
        var result = Thread.CreateUserMessageAdded(content, _utcNow);
        result.Content.ShouldBe(content);
        result.AddedOn.ShouldBe(_utcNow);
    }
}