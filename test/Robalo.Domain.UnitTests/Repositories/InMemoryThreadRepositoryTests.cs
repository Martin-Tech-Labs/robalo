using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using Robalo.Tests.Common;
using Robalo.Common.Models;
using Robalo.Common.Extensions;
using Robalo.Domain.Events;
using System.Data.Common;


namespace Robalo.Domain.UnitTests.Repositories;

public class InMemoryThreadRepositoryTests
{
    private readonly InMemoryThreadRepository _sut = new(Log.Logger);
    private readonly Fixture _fixture = new();


    [Fact]
    public async Task GetThread_ShouldReturnNone_OnNonExistingThread()
    {
        var result = await _sut.GetThread(ThreadIdentifier.NewIdentifier(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }


    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnNonExistingThread()
    {
        var result = await _sut.DeleteThread(ThreadIdentifier.NewIdentifier(), TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task GetMessages_ShouldRetunNone_OnNonExistingThread()
    {
        var result = await _sut.GetMessages(ThreadIdentifier.NewIdentifier(), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task SaveChanges_ShouldSaveNewThread_WithNoMessages()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());
        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<Created>();

        thread.PendingEvents.ShouldBeEmpty();
        thread.OldVersion.HasValue.ShouldBeTrue();
        thread.OldVersion.ShouldBe(thread.NewVersion);
    }

    [Fact]
    public async Task GetThread_ShouldRetrieveThread_WithNoMessages()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
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
        var threadId = ThreadIdentifier.NewIdentifier();
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
        var threadId = ThreadIdentifier.NewIdentifier();
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
        var threadId = ThreadIdentifier.NewIdentifier();
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
        var threadId = ThreadIdentifier.NewIdentifier();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var successOrNone = await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken);
        successOrNone.Value.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task GetThread_ShouldReturnNone_OnDeletedThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.ShouldBeNone();
    }
    [Fact]
    public async Task GetMessages_ShouldReturnNone_OnDeletedThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var messagesOrNone = await _sut.GetMessages(threadId, TestContext.Current.CancellationToken);
        messagesOrNone.ShouldBeNone();
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnDeletedThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        (await _sut.SaveThread(Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>()), TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var result = await _sut.DeleteThread(threadId, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnUpdatingDeletedThread()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
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
        var threadId = ThreadIdentifier.NewIdentifier();
        var newThread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        var @event1 = new UserMessageAdded(_fixture.Create<string>(), newThread.CreatedOn);
        var @event2 = new UserMessageAdded(_fixture.Create<string>(), newThread.CreatedOn.AddSeconds(1));
        var @event3 = new UserMessageAdded(_fixture.Create<string>(), newThread.CreatedOn.AddSeconds(2));

        var events = new[] { @event1, @event2, @event3 };

        foreach (var @event in events)
        {
            newThread.AddUserMessage(@event.Content, @event.AddedOn);
        }

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var threadOrNone = await _sut.GetThread(threadId, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        var thread = threadOrNone.ValueOrFailure;

        thread.Messages.Count.ShouldBe(3);
        for (var i = 0; i < thread.Messages.Count; i++)
        {
            thread.Messages[i].Number.ShouldBe(i + 1);
            thread.Messages[i].Content.ShouldBe(events[i].Content);
            thread.Messages[i].CreatedOn.ShouldBe(events[i].AddedOn);
            thread.Messages[i].Source.ShouldBe(MessageSource.User);
        }
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnCreatingThreadWithExistingId()
    {
        var threadId = ThreadIdentifier.NewIdentifier();

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
        var threadId = ThreadIdentifier.NewIdentifier();

        var originalThread = Thread.NewThread(
                        threadId: threadId,
                        createdOn: _fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var parallelThread = (await _sut.GetThread(threadId, TestContext.Current.CancellationToken)).ValueOrFailure;
        parallelThread.AddUserMessage(_fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(1));

        (await _sut.SaveThread(parallelThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        originalThread.AddUserMessage(_fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(5));

        var result = await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyEnumerable_OnExistingThreadWithNoMessages()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var newThread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var result = await _sut.GetMessages(threadId, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        (await result.ValueOrFailure.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnCorrectMessages_OnExistingThreadWithMessages()
    {
        var threadId = ThreadIdentifier.NewIdentifier();
        var thread = Thread.NewThread(threadId, _fixture.Create<DateTimeOffset>());

        var expectedMessages = new Message[]
        {
            new(1, MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(1)),
            new(2, MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(2)),
            new(3, MessageSource.User, _fixture.Create<string>(), thread.CreatedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

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
            ThreadIdentifier.NewIdentifier(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new Message[]
        {
            new(1, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1)),
            new(2, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2)),
            new(3, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

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
            ThreadIdentifier.NewIdentifier(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(1, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var conflictThread = (await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        expectedMessages.Add(new(2, MessageSource.User, _fixture.Create<string>(), conflictThread.ModifiedOn.AddSeconds(1)));

        conflictThread.AddUserMessage(expectedMessages[1].Content, expectedMessages[1].CreatedOn);

        (await _sut.SaveThread(conflictThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        thread.UpdateTitle(_fixture.Create<string>());
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

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
            ThreadIdentifier.NewIdentifier(),
            _fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(1,MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        expectedMessages.Add(new(2, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1)));

        thread.AddUserMessage(expectedMessages[1].Content, expectedMessages[1].CreatedOn);
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

    [Fact]
    public async Task GetMessage_ShouldReturnNone_OrNonExistingThread()
    {
        var result = await _sut.GetMessage(ThreadIdentifier.NewIdentifier(), 1, TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    public async Task GetMessage_ShouldThrow_IfMessageNumberNotPositive(int number)
    {
        await Should.ThrowAsync<ArgumentOutOfRangeException>(_sut.GetMessage(ThreadIdentifier.NewIdentifier(), number, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetMessage_ShouldReturnNone_OnThreadWithNoMessages()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        var result = await _sut.GetMessage(thread.Id, 1, TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task GetMessage_ShouldReturnNone_OnMessageNumberAboveCount()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());

        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        var result = await _sut.GetMessage(thread.Id, 4, TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public async Task GetMessage_ShouldReturnExpectedMessage(int messageNumber, int index)
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());
        var expectedMessages = new Message[]
        {
            new(1, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1)),
            new(2, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2)),
            new(3, MessageSource.User, _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        var result = await _sut.GetMessage(thread.Id, messageNumber, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        result.ValueOrFailure.ShouldBe(expectedMessages[index]);
    }

    [Fact]
    public async Task QueryMessages_ShouldReturnNone_OnNonExistingThread()
    {
        var result = await _sut.QueryMessages(ThreadIdentifier.NewIdentifier(), Cursor.First(20), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task QueryMessages_ShouldReturnNone_OnDeletedThread()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var result = await _sut.QueryMessages(thread.Id, Cursor.First(20), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task QueryMessages_ShouldReturnEmptyEmpty_OnThreadWithNoMessages()
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        var result = await _sut.QueryMessages(thread.Id, Cursor.First(20), TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();

        var queryResult = result.ValueOrFailure;

        queryResult.Messages.ShouldNotBeNull();
        queryResult.Messages.ShouldBeEmpty();
        queryResult.Prev.ShouldBeNone();
        queryResult.Next.ShouldBeNone();
        queryResult.Self.ShouldBeNone();
    }


    [Theory]
    [InlineData(10, 2, null, new[] { 9, 10 }, new[] { 7, 8 }, new int[] { })]
    [InlineData(10, 3, 9, new[] { 9, 10 }, new[] { 6, 7, 8 }, new int[] { })]
    [InlineData(10, 2, 1, new[] { 1, 2 }, new int[] { }, new int[] { 3, 4 })]
    [InlineData(10, 3, 4, new[] { 4, 5, 6 }, new int[] { 1, 2, 3 }, new int[] { 7, 8, 9 })]
    [InlineData(10, 5, 6, new[] { 6, 7, 8, 9, 10 }, new int[] { 1, 2, 3, 4, 5 }, new int[] { })]
    [InlineData(10, 5, 16, new int[] { }, new int[] { }, new int[] { })]
    [InlineData(10, 10, null, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new int[] { }, new int[] { })]
    [InlineData(10, 20, null, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new int[] { }, new int[] { })]
    public async Task QueryMessages_ShouldProduceExpectedResult(
        int numOfMessages,
        int limit,
        int? reference,
        int[] expectedMessageNumbers,
        int[] expectedMessageNumbersPrev,
        int[] expectedMessageNumbersNext)
    {
        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), _fixture.Create<DateTimeOffset>());

        for (var i = 1; i <= numOfMessages; i++)
        {
            thread.AddUserMessage($"Message_{i}", thread.ModifiedOn.AddSeconds(1));
        }

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var cursor = reference.HasValue ? Cursor.Create(limit, reference.Value) : Cursor.Last(limit);
        var result = await _sut.QueryMessages(thread.Id, cursor, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();

        var queryResult = result.ValueOrFailure;

        if (expectedMessageNumbers.Length > 0)
        {
            queryResult.Messages.ShouldNotBeNull();
            queryResult.Messages.ShouldNotBeEmpty();

            queryResult.Messages.Count.ShouldBe(expectedMessageNumbers.Length);

            var expectedMessages = GenerateExpectedMessages(expectedMessageNumbers);

            queryResult.Messages.ToList().ShouldBeEquivalentTo(expectedMessages);

            queryResult.Self.HasValue.ShouldBeTrue();

            var queryResultSelf = await _sut.QueryMessages(thread.Id, queryResult.Self.ValueOrFailure, TestContext.Current.CancellationToken);
            queryResultSelf.ValueOrFailure.Messages.ShouldBeEquivalentTo(expectedMessages);
        }
        else
        {
            queryResult.Self.ShouldBeNone();
            queryResult.Messages.ShouldBeEmpty();
        }


        if (expectedMessageNumbersPrev.Length > 0)
        {
            queryResult.Prev.HasValue.ShouldBeTrue();
            var queryResultPrev = await _sut.QueryMessages(thread.Id, queryResult.Prev.ValueOrFailure, TestContext.Current.CancellationToken);

            queryResultPrev.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbersPrev));
        }
        else
        {
            queryResult.Prev.ShouldBeNone();
        }

        if (expectedMessageNumbersNext.Length > 0)
        {
            queryResult.Next.HasValue.ShouldBeTrue();
            var queryResultNext = await _sut.QueryMessages(thread.Id, queryResult.Next.ValueOrFailure, TestContext.Current.CancellationToken);

            queryResultNext.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbersNext));
        }
        else
        {
            queryResult.Next.ShouldBeNone();

        }


        List<Message> GenerateExpectedMessages(int[] messageNumbers) =>
           [.. messageNumbers.Select(i => new Message(i, MessageSource.User, $"Message_{i}", thread.CreatedOn.AddSeconds(i)))];


    }
}