using Robalo.Domain.Events;
using Robalo.Domain.Models;
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
        thread.Messages.ShouldBeEmpty();
        thread.PendingEvents.ShouldBeEmpty();
        thread.PendingVersion.ShouldBe(0);
        thread.PersistedVersion.ShouldBeNone();
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

        thread.Messages.Count.ShouldBe(1);

        thread.Messages.Single().Content.ShouldBe(messageContent);
        thread.Messages.Single().CreatedOn.ShouldBe(addedOn);
        thread.Messages.Single().Number.ShouldBe(1);
        thread.Messages.Single().Source.ShouldBe(MessageSource.User);
        thread.Messages.Single().RepliedToMessage.ShouldBeNone();


        thread.PendingVersion.ShouldBe(1);
        thread.PersistedVersion.ShouldBeNone();
        thread.PendingEvents.Count.ShouldBe(1);

        thread.ModifiedOn.ShouldBe(addedOn);

        thread.IsAnswered.ShouldBeFalse();

        thread.PendingEvents.Single().ShouldBeOfType<UserMessageAdded>();
        var @event = (UserMessageAdded)thread.PendingEvents.Single();

        @event.AddedOn.ShouldBe(addedOn);
        @event.Content.ShouldBe(messageContent);
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

        thread.Messages.Count.ShouldBe(2);
        thread.Messages[0].Number.ShouldBe(1);
        thread.Messages[1].Number.ShouldBe(2);

        thread.PendingVersion.ShouldBe(2);
        thread.PersistedVersion.ShouldBeNone();

        thread.ModifiedOn.ShouldBe(createdOn.AddSeconds(2));

        thread.PendingEvents.Count.ShouldBe(2);

        var @event1 = (UserMessageAdded)thread.PendingEvents[0];
        var @event2 = (UserMessageAdded)thread.PendingEvents[1];

        @event1.Content.ShouldBe(thread.Messages[0].Content);
        @event2.Content.ShouldBe(thread.Messages[1].Content);
    }

    [Fact]
    public void AddLlmMessage_ShouldThrowIfNoMessages()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        Should.Throw<InvalidOperationException>(() => thread.AddLlmMessage(_fixture.Create<string>(), 1, thread.CreatedOn));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void AddLlmMessage_ShouldThrowOnEmptyContent(string content)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(content, thread.Messages[0].Number, thread.CreatedOn));
    }

    [Fact]
    public void AddLlmMessage_ShouldThrowOnInvalidDate()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(_fixture.Create<string>(), thread.Messages[0].Number, thread.CreatedOn.AddSeconds(-5)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(3)]
    public void AddLlmMessage_ShouldThrowOnInvalidRepliedToMessage(int repliedToMessage)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        Should.Throw<ArgumentException>(() => thread.AddLlmMessage(_fixture.Create<string>(), repliedToMessage, thread.CreatedOn));
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void AddLlmMessage_ShouldAddLlmMessage_And_Set_Answered_Correctly(int repliedToMessage, bool isAnswered)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);

        var versionBeforeLlmMessage = thread.PendingVersion;
        var content = _fixture.Create<string>();
        var addedOn = thread.CreatedOn.AddSeconds(1);

        thread.AddLlmMessage(content, repliedToMessage, addedOn);

        thread.IsAnswered.ShouldBe(isAnswered);
        thread.Messages.Count.ShouldBe(3);

        thread.PendingVersion.ShouldNotBe(versionBeforeLlmMessage);
        thread.ModifiedOn.ShouldBe(thread.CreatedOn.AddSeconds(1));

        thread.PendingEvents.Count.ShouldBe(3);
        thread.PendingEvents[2].ShouldBeOfType<LlmMessageAdded>();

        var event3 = (LlmMessageAdded)thread.PendingEvents[2];
        event3.AddedOn.ShouldBe(addedOn);
        event3.MessageNumber.ShouldBe(3);
        event3.Content.ShouldBe(content);
        event3.RepliedToMessage.ShouldBe(repliedToMessage);

        thread.Messages.Count.ShouldBe(3);
        thread.Messages[2].Number.ShouldBe(3);
        thread.Messages[2].Content.ShouldBe(content);
        thread.Messages[2].CreatedOn.ShouldBe(addedOn);
        thread.Messages[2].Source.ShouldBe(MessageSource.LLM);
        thread.Messages[2].RepliedToMessage.ShouldBe(repliedToMessage);
    }

    [Fact]
    public void AddUserMessage_ShouldSetAnsweredToFalse_IfWasTrue()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.CreatedOn);
        thread.IsAnswered.ShouldBeFalse();

        thread.AddLlmMessage(_fixture.Create<string>(), 1, thread.CreatedOn);
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

        thread.AddLlmMessage(_fixture.Create<string>(), 2, thread.CreatedOn);
        thread.IsAnswered.ShouldBeTrue();

        thread.AddLlmMessage(_fixture.Create<string>(), 1, thread.CreatedOn);
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

        thread.Messages.Count.ShouldBe(2);
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
                new UserMessageAdded(1, _fixture.Create<string>(), _utcNow),
                new UserMessageAdded(2, _fixture.Create<string>(), _utcNow)
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
        thread.Messages.ShouldBeEmpty();

        thread.PersistedVersion.ShouldBe(0);
        thread.PendingVersion.ShouldBe(0);
    }

    [Fact]
    public void Rehydrate_ShouldSucceed_WithEvents()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var title = _fixture.Create<string>();

        var thread = Thread.Rehydrate(
            identifier: threadId,
            createdOn: _utcNow,
            title: title,
            persistedVersion: 10,
            events: [
                new UserMessageAdded(7, _fixture.Create<string>(),_utcNow.AddSeconds(1)),
                new UserMessageAdded(8, _fixture.Create<string>(),_utcNow.AddSeconds(2)),
                new LlmMessageAdded(9, _fixture.Create<string>(), 2, _utcNow.AddSeconds(3)),
                new UserMessageAdded(10, _fixture.Create<string>(),_utcNow.AddSeconds(4)),
            ]);

        thread.Id.ShouldBe(threadId);
        thread.Title.ShouldBe(title);
        thread.CreatedOn.ShouldBe(_utcNow);
        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(4));

        thread.PendingEvents.ShouldBeEmpty();
        thread.Messages.Count.ShouldBe(4);

        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(10);
        thread.IsAnswered.ShouldBeFalse();


        thread.AddLlmMessage(_fixture.Create<string>(), 4, _utcNow.AddSeconds(5));

        thread.ModifiedOn.ShouldBe(_utcNow.AddSeconds(5));

        thread.PendingEvents.Count.ShouldBe(1);
        thread.Messages.Count.ShouldBe(5);

        thread.PersistedVersion.ShouldBe(10);
        thread.PendingVersion.ShouldBe(11);

        thread.IsAnswered.ShouldBeTrue();

    }


}