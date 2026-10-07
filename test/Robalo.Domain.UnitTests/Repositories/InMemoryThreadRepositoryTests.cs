using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using Robalo.Tests.Common;
using Robalo.Common.Models;
using Robalo.Common.Extensions;
using Robalo.Domain.Events;

namespace Robalo.Domain.UnitTests.Repositories;

public class InMemoryThreadRepositoryTests
{
    private readonly InMemoryThreadRepository _sut = new(Log.Logger);
    private readonly Fixture _fixture = new();

    [Fact]
    public async Task GetThread_ShouldThrowException_OnWrongId()
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.GetThread(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetThread_ShouldReturnNone_OnNonExistingThread()
    {
        var result = await _sut.GetThread(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task DeleteThread_ShouldThrowException_OnWrongId()
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.DeleteThread(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnNonExistingThread()
    {
        var result = await _sut.DeleteThread(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMessages_ShouldThrowException_OnWrongId()
    {
        await Should.ThrowAsync<ArgumentException>(() => _sut.GetMessages(Identifier.NewMessageId(), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMessages_ShouldRetunNone_OnNonExistingThread()
    {
        var result = await _sut.GetMessages(Identifier.NewThreadId(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task SaveChanges_ShouldSaveNewThread_WithNoMessages()
    {
        var thread = Thread.NewThread(Identifier.NewThreadId(), _fixture.Create<DateTimeOffset>());
        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<Created>();

        thread.PendingEvents.ShouldBeEmpty();
        thread.OldVersion.HasValue.ShouldBeTrue();
        thread.OldVersion.ShouldBe(thread.NewVersion);
    }

    [Fact]
    public async Task GetThread_ShouldRetrieveThread_WithNoMessages()
    {
        var threadId = Identifier.NewThreadId();
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread1 = Thread.NewThread(threadId, createdOn);
        (await _sut.SaveThread(thread1, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var version = thread1.NewVersion;

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();

        var thread2 = threadOrNone.ValueOrFailure;

        thread2.ShouldNotBe(thread1);
        thread2.Title.ShouldBeNull();
        thread2.CreatedOn.ShouldBe(thread1.CreatedOn);
        thread2.NewVersion.ShouldBe(thread2.OldVersion);
        thread2.NewVersion.ShouldBe(version);

        thread2.PendingEvents.ShouldBeEmpty();
        thread2.Messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task SaveChanges_ShouldNotSaveThread_WithNoChanges_AfterRetrieval()
    {
        var threadId = Identifier.NewThreadId();
        var thread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var thread2 = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);

        thread2.HasValue.ShouldBeTrue();

        var result = await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NoChanges>();
    }

    [Fact]
    public async Task SaveChanges_ShouldNotSaveThread_WithNoChanges_AfterUpdate()
    {
        var threadId = Identifier.NewThreadId();
        var thread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var thread2 = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        thread2.HasValue.ShouldBeTrue();
        thread2.ValueOrFailure.UpdateTitle(_fixture.Create<string>());
        (await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();
        (await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<NoChanges>();
    }

    [Fact]
    public async Task SaveChanges_ShouldUpdateTitle()
    {
        var threadId = Identifier.NewThreadId();
        var createdOn = _fixture.Create<DateTimeOffset>();
        var newThread = Thread.NewThread(threadId, createdOn);
        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);

        threadOrNone.HasValue.ShouldBeTrue();

        var thread = threadOrNone.ValueOrFailure;

        var title = _fixture.Create<string>();

        thread.UpdateTitle(title);
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        var updatedThreadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        updatedThreadOrNone.HasValue.ShouldBeTrue();

        var updatedThread = updatedThreadOrNone.ValueOrFailure;

        updatedThread.Id.ShouldBe(threadId);
        updatedThread.CreatedOn.ShouldBe(createdOn);
        updatedThread.Title.ShouldBe(title);
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnSuccess_OnExistingThread()
    {
        var threadId = Identifier.NewThreadId();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var successOrNone = await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken);
        successOrNone.Value.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task GetThread_ShouldReturnNone_OnDeletedThread()
    {
        var threadId = Identifier.NewThreadId();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.ShouldBeNone();
    }
    [Fact]
    public async Task GetMessages_ShouldReturnNone_OnDeletedThread()
    {
        var threadId = Identifier.NewThreadId();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var messagesOrNone = await _sut.GetMessages(threadId, TestContext.Current.CancellationToken);
        messagesOrNone.ShouldBeNone();
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnDeletedThread()
    {
        var threadId = Identifier.NewThreadId();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var result = await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnUpdatingDeletedThread()
    {
        var threadId = Identifier.NewThreadId();
        var thread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        thread.UpdateTitle(_fixture.Create<string>());

        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task SaveThread_ShouldCorrectSaveEvents()
    {
        var threadId = Identifier.NewThreadId();
        var newThread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        var @event1 = new UserMessageAdded(Identifier.NewMessageId(), _fixture.Create<string>(), newThread.CreatedOn);
        var @event2 = new UserMessageAdded(Identifier.NewMessageId(), _fixture.Create<string>(), newThread.CreatedOn.AddSeconds(1));
        var @event3 = new UserMessageAdded(Identifier.NewMessageId(), _fixture.Create<string>(), newThread.CreatedOn.AddSeconds(2));

        var events = new[] { @event1, @event2, @event3 };

        foreach (var @event in events)
        {
            newThread.AddUserMessage(@event.Id, @event.Content, @event.AddedOn);
        }

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        var thread = threadOrNone.ValueOrFailure;

        thread.Messages.Count.ShouldBe(3);
        for (var i = 0; i < thread.Messages.Count; i++)
        {
            thread.Messages[i].Id.ShouldBe(events[i].Id);
            thread.Messages[i].Content.ShouldBe(events[i].Content);
            thread.Messages[i].CreatedOn.ShouldBe(events[i].AddedOn);
            thread.Messages[i].Source.ShouldBe(MessageSource.User);
        }
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnCreatingThreadWithExistingId()
    {
        var threadId = Identifier.NewThreadId();

        (await _sut.SaveThread(
            thread: Thread.NewThread(
                threadId: threadId,
                createdOn: _fixture.Create<DateTimeOffset>()),
            cancellationToken: TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var newThread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        var result = await _sut.SaveThread(newThread, TestContext.Current.CancellationToken);

        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnUpdatingThreadWithInParallel()
    {
        var threadId = Identifier.NewThreadId();

        var originalThread = Thread.NewThread(
                        threadId: threadId,
                        createdOn: _fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var parallelThread = (await _sut.GetThread(threadId, TestContext.Current.CancellationToken)).ValueOrFailure;
        parallelThread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(1));

        (await _sut.SaveThread(parallelThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        originalThread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(5));

        var result = await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyEnumerable_OnExistingThreadWithNoMessages()
    {
        var threadId = Identifier.NewThreadId();
        var newThread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var result = await _sut.GetMessages(threadId, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        (await result.ValueOrFailure.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnCorrectMessages_OnExistingThreadWithMessages()
    {
        var threadId = Identifier.NewThreadId();
        var thread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        var expectedMessages = new Message[]
        {
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(1)),
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(2)),
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Id, e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var result = await _sut.GetMessages(threadId, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        var messages = await result.ValueOrFailure.ToArrayAsync(TestContext.Current.CancellationToken);
        messages.ShouldBeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task SaveThread_OnConcurrencyConflictOnCreation_ShouldNotPersistConflictingChanges()
    {
        var thread = Thread.NewThread(
            Identifier.NewThreadId(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new Message[]
        {
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1)),
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2)),
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Id, e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var conflictThread = Thread.NewThread(
            thread.Id,
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        (await _sut.SaveThread(conflictThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<ConcurrencyConflict>();

        var threadOrNone = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        threadOrNone.ValueOrFailure.Title.ShouldBe(thread.Title);
        threadOrNone.ValueOrFailure.CreatedOn.ShouldBe(thread.CreatedOn);
        threadOrNone.ValueOrFailure.ModifiedOn.ShouldBe(thread.ModifiedOn);

        threadOrNone.ValueOrFailure.Messages.ToArray().ShouldBeEquivalentTo(expectedMessages);

        var messagesOrNone = await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken);
        messagesOrNone.HasValue.ShouldBeTrue();
        var messages = await messagesOrNone.ValueOrFailure.ToArrayAsync(TestContext.Current.CancellationToken);
        messages.ShouldBeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task SaveThread_OnConcurrencyConflictOnUpdate_ShouldNotPersistConflictingChanges()
    {
        var thread = Thread.NewThread(
            Identifier.NewThreadId(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Id, e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var conflictThread = (await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        expectedMessages.Add(new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), conflictThread.ModifiedOn.AddSeconds(1)));

        conflictThread.AddUserMessage(expectedMessages[1].Id, expectedMessages[1].Content, expectedMessages[1].CreatedOn);

        (await _sut.SaveThread(conflictThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        thread.UpdateTitle(_fixture.Create<string>());
        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(Identifier.NewMessageId(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<ConcurrencyConflict>();

        var threadOrNone = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        threadOrNone.ValueOrFailure.Title.ShouldBe(conflictThread.Title);
        threadOrNone.ValueOrFailure.CreatedOn.ShouldBe(conflictThread.CreatedOn);
        threadOrNone.ValueOrFailure.ModifiedOn.ShouldBe(conflictThread.ModifiedOn);

        threadOrNone.ValueOrFailure.Messages.ToArray().ShouldBeEquivalentTo(expectedMessages.ToArray());

        var messagesOrNone = await _sut.GetMessages(conflictThread.Id, TestContext.Current.CancellationToken);
        messagesOrNone.HasValue.ShouldBeTrue();
        var messages = await messagesOrNone.ValueOrFailure.ToArrayAsync(TestContext.Current.CancellationToken);
        messages.ShouldBeEquivalentTo(expectedMessages.ToArray());
    }

    [Fact]
    public async Task SaveThread_ShouldCorrectlyHandleSubsequentUpdates()
    {
        var newTitle = _fixture.Create<string>();
        var thread = Thread.NewThread(
            Identifier.NewThreadId(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Id, e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        expectedMessages.Add(new(Identifier.NewMessageId(), MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1)));

        thread.AddUserMessage(expectedMessages[1].Id, expectedMessages[1].Content, expectedMessages[1].CreatedOn);
        thread.UpdateTitle(newTitle);

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();


        var threadOrNone = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        threadOrNone.ValueOrFailure.Title.ShouldBe(newTitle);
        threadOrNone.ValueOrFailure.CreatedOn.ShouldBe(thread.CreatedOn);
        threadOrNone.ValueOrFailure.ModifiedOn.ShouldBe(thread.ModifiedOn);

        threadOrNone.ValueOrFailure.Messages.ToArray().ShouldBeEquivalentTo(expectedMessages.ToArray());

        var messagesOrNone = await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken);
        messagesOrNone.HasValue.ShouldBeTrue();
        var messages = await messagesOrNone.ValueOrFailure.ToArrayAsync(TestContext.Current.CancellationToken);
        messages.ShouldBeEquivalentTo(expectedMessages.ToArray());
    }
}