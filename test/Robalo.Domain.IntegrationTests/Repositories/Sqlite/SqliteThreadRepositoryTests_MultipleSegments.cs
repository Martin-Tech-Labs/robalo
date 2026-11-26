using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Dto;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Repositories.Sqlite;

public class SqliteThreadRepositoryTests_MultipleSegments : IClassFixture<DatabaseFixture>
{
    readonly SqliteThreadRepository _sut;
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;
    readonly Mock<IMessageBus> _messageBus = new();

    public SqliteThreadRepositoryTests_MultipleSegments(DatabaseFixture fixture)
    {
        _sut = new SqliteThreadRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new(fixture.Database, fixture.Password)),
            _messageBus.Object);

        Extensions.AddDapperTypeHandlers();
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_BreakMessage_WithEmptySegment()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.ShouldBeEquivalentTo(thread);
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_BreakMessage_WithNonEmptySegment()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.ShouldBeEquivalentTo(thread);
    }

    [Fact]
    public async Task ShouldCorrectly_Save_And_Load_SummaryMessage()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddSummaryMessage(_fixture.Create<string>(), thread.ExtendedContext.Select(c => c.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        Assert.Equivalent(thread, thread2, strict: true);
        thread.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread2.Context.Select(c => c.MessageId));
    }

    [Fact]
    public async Task RefreshThread_ShouldCorrectly_Handle_Segments()
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        await _sut.RefreshThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();
        thread.Context.ShouldBeEmpty();
        thread.IsAnswered.ShouldBeTrue();
        thread.ModifiedOn.ShouldBe(thread2.ModifiedOn);
        Assert.Equivalent(thread2, thread, strict: true);
        thread2.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread.Context.Select(c => c.MessageId));
        thread2.AddUserMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        thread2.AddAssistantMessage(_fixture.Create<string>(), thread2.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        thread2.AddSummaryMessage(_fixture.Create<string>(), thread2.ExtendedContext.Select(m => m.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        foreach (var msg in thread2.ExtendedContext)
        {
            thread2.AddCopyOfMessage(msg, thread2.ModifiedOn.AddSeconds(1));
        }

        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        thread2.AddBreakMessage(_fixture.Create<string>(), thread2.ModifiedOn.AddSeconds(1));
        foreach (var msg in thread2.ExtendedContext)
        {
            thread2.AddCopyOfMessage(msg, thread2.ModifiedOn.AddSeconds(1));
        }
        await _sut.SaveThread(thread2, TestContext.Current.CancellationToken);
        await _sut.RefreshThread(thread, TestContext.Current.CancellationToken);
        Assert.Equivalent(thread2, thread, strict: true);
        thread2.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread.Context.Select(c => c.MessageId));
    }

    [Fact]
    public async Task ComplexTest_With_Save_Load_and_Get_Messages()
    {
        var events = new List<MessageAdded>();
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1))
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(2));

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddSummaryMessage(_fixture.Create<string>(), thread.ExtendedContext.Select(c => c.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[0], thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[1], thread.ModifiedOn.AddSeconds(1));
        thread.AddCopyOfMessage(thread.ExtendedContext[2], thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.ExtendedContext[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(3));
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();
        contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        foreach (var msg in thread.ExtendedContext)
        {
            thread.AddCopyOfMessage(msg, thread.ModifiedOn.AddSeconds(1));
        }
        thread.AddSummaryMessage(_fixture.Create<string>(), new List<MessageIdentifier> { thread.ExtendedContext[0].MessageId }, _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        events.AddRange(thread.PendingEvents);
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        Should.Throw<InvalidOperationException>((() => thread.AddSummaryMessage(_fixture.Create<string>(), contextBeforeBreak.Select(c => c.MessageId).ToList(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))));
        Should.Throw<InvalidOperationException>((() => thread.AddCopyOfMessage(contextBeforeBreak[0], thread.ModifiedOn.AddSeconds(1))));
        Should.Throw<ArgumentException>((() => thread.AddAssistantMessage(_fixture.Create<string>(), contextBeforeBreak[0].MessageId, _fixture.Create<int>(), _fixture.Create<int>(), _fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1))));

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        Assert.Equivalent(thread, thread2, strict: true);
        thread.Context.Select(c => c.MessageId).ShouldBeEquivalentTo(thread2.Context.Select(c => c.MessageId));

        var messages = await (await _sut.GetMessages(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure.ToListAsync(TestContext.Current.CancellationToken);
        var i = 0;
        messages.ShouldBeEquivalentTo(events.Select(e => Message.FromModel(e, thread.Id, ++i)).ToList());
    }
}