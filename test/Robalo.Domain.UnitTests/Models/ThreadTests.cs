using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Robalo.Tests.Common;

namespace Robalo.Domain.UnitTests.Models;

public class ThreadTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void NewThread_ShouldThrowOnInvalidId()
    {
        Should.Throw<ArgumentException>(() => Thread.NewThread(Identifier.NewMessageId(), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void NewThread_ShouldThrowOnInvalidDate()
    {
        Should.Throw<ArgumentException>(() => Thread.NewThread(Identifier.NewThreadId(), default));
    }

    [Fact]
    public void NewThread_ShouldCreateThread()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var id = Identifier.NewThreadId();

        var thread = Thread.NewThread(id, createdOn);
        thread.CreatedOn.ShouldBe(createdOn);
        thread.ModifiedOn.ShouldBe(createdOn);
        thread.Id.ShouldBe(id);
        thread.Messages.ShouldBeEmpty();
        thread.PendingEvents.ShouldBeEmpty();
        thread.NewVersion.ShouldNotBeNull();
        thread.OldVersion.ShouldBeNone();
    }

    [Fact]
    public void AddUserMessaged_ShouldThrowOnWrongIdType()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        Should.Throw<ArgumentException>(() => thread.AddUserMessage(Identifier.NewThreadId(), _fixture.Create<string>(), createdOn.AddSeconds(1)));
    }

    [Fact]
    public void AddUserMessaged_ShouldThrowOnWrongDate()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        Should.Throw<ArgumentException>(() => thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn.AddSeconds(-1)));
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]

    public void AddUserMessaged_ShouldThrowOnEmptyMessage(string messageContent)
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        Should.Throw<ArgumentException>(() => thread.AddUserMessage(Identifier.NewMessageId(), messageContent, createdOn.AddSeconds(1)));
    }


    [Fact]
    public void AddUserMessage_ShouldAddMessage()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);
        var oldVersion = thread.OldVersion;

        var messageId = Identifier.NewMessageId();
        var messageContent = _fixture.Create<string>();
        var addedOn = createdOn.AddSeconds(1);

        thread.AddUserMessage(messageId, messageContent, addedOn);

        thread.Messages.Count.ShouldBe(1);

        thread.Messages.Single().Content.ShouldBe(messageContent);
        thread.Messages.Single().CreatedOn.ShouldBe(addedOn);
        thread.Messages.Single().Id.ShouldBe(messageId);
        thread.Messages.Single().Source.ShouldBe(MessageSource.User);

        thread.NewVersion.ShouldNotBe(thread.OldVersion);
        thread.OldVersion.ShouldBe(oldVersion);
        thread.PendingEvents.Count.ShouldBe(1);

        thread.ModifiedOn.ShouldBe(addedOn);

        thread.PendingEvents.Single().ShouldBeOfType<UserMessageAdded>();
        var @event = (UserMessageAdded)thread.PendingEvents.Single();

        @event.AddedOn.ShouldBe(addedOn);
        @event.Content.ShouldBe(messageContent);
        @event.Id.ShouldBe(messageId);
    }

    [Fact]
    public void AddSecondUserMessage_ShouldFailOnInvalidDate()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn.AddSeconds(1));
        Should.Throw<ArgumentException>(() => thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn));
    }

    [Fact]
    public void AddTwoUserMessages_ShouldAddMessages()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);
        var oldVersion = thread.OldVersion;

        var messageId1 = Identifier.NewMessageId();
        var messageId2 = Identifier.NewMessageId();

        thread.AddUserMessage(messageId1, _fixture.Create<string>(), createdOn.AddSeconds(1));
        var newVersion1 = thread.NewVersion;
        thread.AddUserMessage(messageId2, _fixture.Create<string>(), createdOn.AddSeconds(2));

        thread.Messages.Count.ShouldBe(2);
        thread.Messages[0].Id.ShouldBe(messageId1);
        thread.Messages[1].Id.ShouldBe(messageId2);

        thread.NewVersion.ShouldNotBe(thread.OldVersion);
        thread.NewVersion.ShouldNotBe(newVersion1);
        thread.OldVersion.ShouldBe(oldVersion);

        thread.ModifiedOn.ShouldBe(createdOn.AddSeconds(2));

        thread.PendingEvents.Count.ShouldBe(2);

        var @event1 = (UserMessageAdded)thread.PendingEvents[0];
        var @event2 = (UserMessageAdded)thread.PendingEvents[1];

        @event1.Id.ShouldBe(messageId1);
        @event2.Id.ShouldBe(messageId2);
    }

    [Fact]
    public void MarkAggregateAsSynchronized_ShouldCleanPendingEventsAndUpdateVersion()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn);
        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn);

        var newVersion = thread.NewVersion;

        thread.MarkAggregateAsSynchronized();
        thread.OldVersion.ShouldBe(newVersion);
        thread.OldVersion.ShouldBe(thread.NewVersion);

        thread.Messages.Count.ShouldBe(2);
        thread.PendingEvents.ShouldBeEmpty();
    }

    [Fact]
    public void MarkAggregateAsSynchronized_ShouldOverrideVersionIfProvided()
    {
        var versionToOverride = Version.NewVersion();
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread = Thread.NewThread(Identifier.NewThreadId(), createdOn);

        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn);
        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), createdOn);       

        thread.MarkAggregateAsSynchronized(versionToOverride);
        thread.OldVersion.ShouldBe(versionToOverride);
        thread.NewVersion.ShouldBe(versionToOverride);

        thread.Messages.Count.ShouldBe(2);
        thread.PendingEvents.ShouldBeEmpty();
    }

    [Fact]
    public void UpdateTitle_ShouldUpdateTitle()
    {
        var thread = Thread.NewThread(Identifier.NewThreadId(), _fixture.Create<DateTimeOffset>());
        var title = _fixture.Create<string>();
        var modifiedOn = thread.ModifiedOn;
        thread.UpdateTitle(title);
        thread.Title.ShouldBe(title);
        thread.NewVersion.ShouldNotBe(thread.OldVersion);
        thread.ModifiedOn.ShouldBe(modifiedOn);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void UpdateTitle_ShouldThrowOnBlankTitle(string title)
    {
        var thread = Thread.NewThread(Identifier.NewThreadId(), _fixture.Create<DateTimeOffset>());
        Should.Throw<ArgumentException>(() => thread.UpdateTitle(title));
    }
}