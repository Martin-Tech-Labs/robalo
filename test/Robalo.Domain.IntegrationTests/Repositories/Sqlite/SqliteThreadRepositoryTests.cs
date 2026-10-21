using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Robalo.Common.Models;
using Robalo.Domain.IntegrationTests;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Tests.Common;
using Serilog;
using Robalo.Common.Extensions;
using Robalo.Domain.Events;

namespace Robalo.Domain.IntegrationTests.Repositories;

public class SqliteThreadRepositoryTests : IClassFixture<DatabaseFixture>
{

    private readonly SqliteThreadRepository _sut;
    private readonly Fixture _fixture = new();

    public SqliteThreadRepositoryTests(DatabaseFixture fixture)
    {
        _sut = new SqliteThreadRepository(Log.Logger, new SqliteConnectionFactory(Log.Logger, new(fixture.Database, fixture.Password)));
        Extensions.AddDapperTypeHandlers();
    }


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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<Created>();

        thread.PendingEvents.ShouldBeEmpty();
        thread.OldVersion.HasValue.ShouldBeTrue();
        thread.OldVersion.ShouldBe(thread.NewVersion);
    }

    [Fact]
    public async Task GetThread_ShouldRetrieveThread_WithNoMessages()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var thread1 = Thread.NewThread(createdOn);
        (await _sut.SaveThread(thread1, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var version = thread1.NewVersion;

        var threadOrNone = await _sut.GetThread(thread1.Id, TestContext.Current.CancellationToken);
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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var thread2 = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);

        thread2.HasValue.ShouldBeTrue();

        var result = await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NoChanges>();
    }

    [Fact]
    public async Task SaveChanges_ShouldNotSaveThread_WithNoChanges_AfterUpdate()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var thread2 = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);
        thread2.HasValue.ShouldBeTrue();
        thread2.ValueOrFailure.UpdateTitle(_fixture.Create<string>());
        (await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();
        (await _sut.SaveThread(thread2.ValueOrFailure, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<NoChanges>();
    }

    [Fact]
    public async Task SaveChanges_ShouldUpdateTitle()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();
        var newThread = Thread.NewThread(createdOn);
        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var threadOrNone = await _sut.GetThread(newThread.Id, TestContext.Current.CancellationToken);

        threadOrNone.HasValue.ShouldBeTrue();

        var thread = threadOrNone.ValueOrFailure;

        var title = _fixture.Create<string>();

        thread.UpdateTitle(title);
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        var updatedThreadOrNone = await _sut.GetThread(newThread.Id, TestContext.Current.CancellationToken);
        updatedThreadOrNone.HasValue.ShouldBeTrue();

        var updatedThread = updatedThreadOrNone.ValueOrFailure;

        updatedThread.Id.ShouldBe(newThread.Id);
        updatedThread.CreatedOn.ShouldBe(createdOn);
        updatedThread.Title.ShouldBe(title);
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnSuccess_OnExistingThread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var successOrNone = await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken);
        successOrNone.Value.ShouldBeOfType<Success>();
    }

    [Fact]
    public async Task GetThread_ShouldReturnNone_OnDeletedThread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var threadOrNone = await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken);
        threadOrNone.ShouldBeNone();
    }
    [Fact]
    public async Task GetMessages_ShouldReturnNone_OnDeletedThread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var messagesOrNone = await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken);
        messagesOrNone.ShouldBeNone();
    }

    [Fact]
    public async Task DeleteThread_ShouldReturnNotFound_OnDeletedThread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var result = await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<NotFound>();
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnUpdatingDeletedThread()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        thread.UpdateTitle(_fixture.Create<string>());

        var result = await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task SaveThread_ShouldCorrectSaveEvents()
    {
        var newThread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        var event1 = new UserMessageAdded(1, _fixture.Create<string>(), newThread.CreatedOn);
        var event2 = new UserMessageAdded(2, _fixture.Create<string>(), newThread.CreatedOn.AddSeconds(1));
        var event3 = new UserMessageAdded(3, _fixture.Create<string>(), newThread.CreatedOn.AddSeconds(2));
        var event4 = new LlmMessageAdded(4, _fixture.Create<string>(), 3, newThread.CreatedOn.AddSeconds(3));
        var event5 = new UserMessageAdded(5, _fixture.Create<string>(), newThread.CreatedOn.AddSeconds(4));
        var event6 = new LlmMessageAdded(6, _fixture.Create<string>(), 5, newThread.CreatedOn.AddSeconds(5));

        var events = new MessageAdded[] { event1, event2, event3, event4, event5, event6 };

        foreach (var @event in events)
        {
            _ = @event switch
            {
                UserMessageAdded userMessageAdded => newThread.AddUserMessage(userMessageAdded.Content, userMessageAdded.AddedOn),
                LlmMessageAdded llmMessageAdded => newThread.AddLlmMessage(llmMessageAdded.Content, llmMessageAdded.RepliedToMessage, llmMessageAdded.AddedOn)
            };
        }

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var threadOrNone = await _sut.GetThread(newThread.Id, TestContext.Current.CancellationToken);
        threadOrNone.HasValue.ShouldBeTrue();
        var thread = threadOrNone.ValueOrFailure;

        thread.Messages.Count.ShouldBe(events.Length);
        for (var i = 0; i < thread.Messages.Count; i++)
        {
            thread.Messages[i].Number.ShouldBe(i + 1);
            thread.Messages[i].Content.ShouldBe(events[i].Content);
            thread.Messages[i].CreatedOn.ShouldBe(events[i].AddedOn);

            if (events[i] is UserMessageAdded userMessageAdded)
            {
                thread.Messages[i].Source.ShouldBe(MessageSource.User);
                thread.Messages[i].RepliedToMessage.ShouldBeNone();
            }
            else if (events[i] is LlmMessageAdded llmMessageAdded)
            {
                thread.Messages[i].Source.ShouldBe(MessageSource.LLM);
                thread.Messages[i].RepliedToMessage.ShouldBe(llmMessageAdded.RepliedToMessage);
            }
            else
            {
                throw new InvalidOperationException("Unexpected type of message");
            }
        }
    }

    [Fact]
    public async Task SaveThread_ShouldReturnConcurrencyConflict_OnUpdatingThreadWithInParallel()
    {

        var originalThread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var parallelThread = (await _sut.GetThread(originalThread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        parallelThread.AddUserMessage(_fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(1));

        (await _sut.SaveThread(parallelThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Updated>();

        originalThread.AddUserMessage(_fixture.Create<string>(), originalThread.CreatedOn.AddSeconds(5));

        var result = await _sut.SaveThread(originalThread, TestContext.Current.CancellationToken);
        result.Value.ShouldBeOfType<ConcurrencyConflict>();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnEmptyEnumerable_OnExistingThreadWithNoMessages()
    {
        var newThread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        (await _sut.SaveThread(newThread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var result = await _sut.GetMessages(newThread.Id, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        (await result.ValueOrFailure.AnyAsync(TestContext.Current.CancellationToken)).ShouldBeFalse();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnCorrectMessages_OnExistingThreadWithMessages()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

        var expectedMessages = new Message[]
        {
            new(1, MessageSource.User, _fixture.Create<string>(), None.Default, thread.CreatedOn.AddSeconds(1)),
            new(2, MessageSource.User, _fixture.Create<string>(), None.Default, thread.CreatedOn.AddSeconds(2)),
            new(3, MessageSource.User, _fixture.Create<string>(), None.Default, thread.CreatedOn.AddSeconds(3)),
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var result = await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken);
        result.HasValue.ShouldBeTrue();
        var messages = await result.ValueOrFailure.ToArrayAsync(TestContext.Current.CancellationToken);
        messages.ShouldBeEquivalentTo(expectedMessages);
    }

    [Fact]
    public async Task SaveThread_OnConcurrencyConflictOnUpdate_ShouldNotPersistConflictingChanges()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(1, MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        var conflictThread = (await _sut.GetThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        expectedMessages.Add(new(2, MessageSource.User, _fixture.Create<string>(), None.Default, conflictThread.ModifiedOn.AddSeconds(1)));

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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>()).UpdateTitle(_fixture.Create<string>());

        var expectedMessages = new List<Message>
        {
            new(1,MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(1))
        };

        thread.ForEach(expectedMessages, (t, e) => thread.AddUserMessage(e.Content, e.CreatedOn));

        (await _sut.SaveThread(thread, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Created>();

        expectedMessages.Add(new(2, MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(1)));

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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        var result = await _sut.GetMessage(thread.Id, 1, TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task GetMessage_ShouldReturnNone_OnMessageNumberAboveCount()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        var expectedMessages = new Message[]
        {
            new(1, MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(1)),
            new(2, MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(2)),
            new(3, MessageSource.User, _fixture.Create<string>(), None.Default, thread.ModifiedOn.AddSeconds(3)),
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
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);

        (await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken)).Value.ShouldBeOfType<Success>();

        var result = await _sut.QueryMessages(thread.Id, Cursor.First(20), TestContext.Current.CancellationToken);
        result.ShouldBeNone();
    }

    [Fact]
    public async Task QueryMessages_ShouldReturnEmptyEmpty_OnThreadWithNoMessages()
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());
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

#pragma warning disable xUnit1026

    [Theory]
    [InlineData(1, 11, 10, null, new[] { 2, 3, 4, 5, 6, 7, 8, 9, 10, 11 }, new[] { 1 }, new int[] { })]
    [InlineData(2, 10, 2, null, new[] { 9, 10 }, new[] { 7, 8 }, new int[] { })]
    [InlineData(3, 10, 3, 9, new[] { 9, 10 }, new[] { 6, 7, 8 }, new int[] { })]
    [InlineData(4, 10, 2, 1, new[] { 1, 2 }, new int[] { }, new int[] { 3, 4 })]
    [InlineData(5, 10, 3, 4, new[] { 4, 5, 6 }, new int[] { 1, 2, 3 }, new int[] { 7, 8, 9 })]
    [InlineData(6, 10, 5, 6, new[] { 6, 7, 8, 9, 10 }, new int[] { 1, 2, 3, 4, 5 }, new int[] { })]
    [InlineData(7, 10, 5, 16, new int[] { }, new int[] { }, new int[] { })]
    [InlineData(8, 10, 10, null, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new int[] { }, new int[] { })]
    [InlineData(9, 10, 20, null, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }, new int[] { }, new int[] { })]
    public async Task QueryMessages_ShouldProduceExpectedResult(
        int testCase,
        int numOfMessages,
        int limit,
        int? reference,
        int[] expectedMessageNumbers,
        int[] expectedMessageNumbersPrev,
        int[] expectedMessageNumbersNext)
    {
        var thread = Thread.NewThread(_fixture.Create<DateTimeOffset>());

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
            queryResultSelf.HasValue.ShouldBeTrue();
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

            queryResultPrev.HasValue.ShouldBeTrue();
            queryResultPrev.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbersPrev));

            queryResultPrev.ValueOrFailure.Next.HasValue.ShouldBeTrue();

            var queryNextFromPrev = await _sut.QueryMessages(thread.Id, queryResultPrev.ValueOrFailure.Next.ValueOrFailure, TestContext.Current.CancellationToken);

            queryNextFromPrev.HasValue.ShouldBeTrue(nameof(queryNextFromPrev));
            queryNextFromPrev.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbers));
        }
        else
        {
            queryResult.Prev.ShouldBeNone();
        }

        if (expectedMessageNumbersNext.Length > 0)
        {
            queryResult.Next.HasValue.ShouldBeTrue();
            var queryResultNext = await _sut.QueryMessages(thread.Id, queryResult.Next.ValueOrFailure, TestContext.Current.CancellationToken);

            queryResultNext.HasValue.ShouldBeTrue();
            queryResultNext.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbersNext));

            var queryPrevFromNext = await _sut.QueryMessages(thread.Id, queryResultNext.ValueOrFailure.Prev.ValueOrFailure, TestContext.Current.CancellationToken);
            queryPrevFromNext.HasValue.ShouldBeTrue();
            queryPrevFromNext.ValueOrFailure.Messages.ShouldBeEquivalentTo(GenerateExpectedMessages(expectedMessageNumbers));
        }
        else
        {
            queryResult.Next.ShouldBeNone();
        }


        List<Message> GenerateExpectedMessages(int[] messageNumbers) =>
           [.. messageNumbers.Select(i => new Message(i, MessageSource.User, $"Message_{i}", None.Default, thread.CreatedOn.AddSeconds(i)))];
    }

#pragma warning restore


    [Fact]
    public async Task ListThreads_ShouldReturnThreadInfos()
    {
        var createdOn = _fixture.Create<DateTimeOffset>();

        var thread1 = Thread.NewThread(createdOn)
            .UpdateTitle(_fixture.Create<string>())
            .AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), createdOn.AddSeconds(2));

        var thread2 = Thread.NewThread(createdOn.AddSeconds(3));

        await _sut.SaveThread(thread1, TestContext.Current.CancellationToken);
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);

        var result = await _sut.ListThreads(TestContext.Current.CancellationToken);

        result.Count.ShouldBeGreaterThanOrEqualTo(2);

        var threads = new[] { thread1, thread2 };

        var list = result.Where(t => t.Id == thread1.Id || t.Id == thread2.Id).OrderBy(r => r.CreatedOn).ToList();

        for (int i = 0; i < list.Count; i++)
        {
            var threadInfo = list[i];
            var thread = threads[i];
            threadInfo.Id.ShouldBe(thread.Id);
            threadInfo.CreatedOn.ShouldBe(thread.CreatedOn);
            threadInfo.ModifiedOn.ShouldBe(thread.ModifiedOn);
            threadInfo.Title.ShouldBe(thread.Title);
        }
    }

    [Fact]
    public async Task ListThreads_ShouldReturnEmptyOnNoThreads()
    {
        foreach (var thread in await _sut.ListThreads(TestContext.Current.CancellationToken))
        {
            await _sut.DeleteThread(thread.Id, TestContext.Current.CancellationToken);
        }
        var result = await _sut.ListThreads(TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }
}