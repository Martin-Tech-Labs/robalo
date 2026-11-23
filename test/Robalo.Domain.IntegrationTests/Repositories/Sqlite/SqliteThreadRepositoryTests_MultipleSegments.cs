using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Repositories.Sqlite;
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

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.ModifiedOn.AddSeconds(1));

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

        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.ModifiedOn.AddSeconds(1));

        thread.ExtendedContext.ShouldBeEmpty();
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        var contextBeforeBreak = thread.Context.ToList();
        thread.AddBreakMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.ExtendedContext.ToList().ShouldBeEquivalentTo(contextBeforeBreak);
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(2));
        thread.AddAssistantMessage(_fixture.Create<string>(), thread.Context[0].MessageId, thread.ModifiedOn.AddSeconds(3));
        await _sut.SaveThread(thread, TestContext.Current.CancellationToken);
        thread.ExtendedContext.ShouldBeEmpty();

        var thread2 = (await _sut.LoadThread(thread.Id, TestContext.Current.CancellationToken)).ValueOrFailure;
        thread2.ShouldBeEquivalentTo(thread);
    }
}