using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Streaming;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Streaming;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Streaming;

public class StreamingServiceTests : IClassFixture<DatabaseFixture>, IDisposable
{
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;
    readonly StreamingService<ThreadIdentifier, Message, int> _sut;
    readonly SqliteThreadRepository _repository;
    public StreamingServiceTests(DatabaseFixture databaseFixture)
    {
        _repository = new SqliteThreadRepository(
              Log.Logger,
              new SqliteConnectionFactory(Log.Logger, new(databaseFixture.Database, databaseFixture.Password)),
              new Mock<IMessageBus>().Object);

        Extensions.AddDapperTypeHandlers();

        var streamingProvider = new ThreadStreamingProvider(Log.Logger, _repository);
        _utcNow = _fixture.Create<DateTimeOffset>();

        _sut = new(Log.Logger, streamingProvider);
    }

    [Fact]
    async Task Pull_ShouldCorrectlyReplayMessagesByThreadId()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var thread1 = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1));
        thread1.AddLlmMessage(_fixture.Create<string>(), thread1.Messages[^1], thread1.ModifiedOn.AddSeconds(1));
        await _repository.SaveThread(thread1, TestContext.Current.CancellationToken);

        var thread2 = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow.AddSeconds(1));
        thread2.AddLlmMessage(_fixture.Create<string>(), thread2.Messages[^1], thread2.ModifiedOn.AddSeconds(1));
        await _repository.SaveThread(thread2, TestContext.Current.CancellationToken);

        var list1 = await _sut.Pull(thread1.Id, 1, timeout.Token).Take(2).ToListAsync(timeout.Token);
        var list2 = await _sut.Pull(thread2.Id, 2, timeout.Token).Take(1).ToListAsync(timeout.Token);

        list1.ShouldNotBeNull();
        list2.ShouldNotBeNull();

        list1.Select(l => l.Event).ToList().ShouldBeEquivalentTo(thread1.Messages.ToList()[1..]);
        list2.Select(l => l.Event).ToList().ShouldBeEquivalentTo(thread2.Messages.ToList()[2..]);
    }

    public void Dispose()
    {
        _sut.Dispose();
        GC.SuppressFinalize(this);
    }
}