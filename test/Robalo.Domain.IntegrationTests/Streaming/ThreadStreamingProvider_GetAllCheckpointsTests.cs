using Moq;
using Robalo.Common.Coordination;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Streaming;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Streaming;

public class ThreadStreamingProvider_GetAllCheckpointsTests : IDisposable
{
    readonly ThreadStreamingProvider _sut;
    readonly SqliteThreadRepository _repository;
    readonly DatabaseFixture _databaseFixture = new();
    readonly DateTimeOffset _utcNow;
    readonly Fixture _fixture = new();

    public ThreadStreamingProvider_GetAllCheckpointsTests()
    {
        _repository = new SqliteThreadRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new(_databaseFixture.Database, _databaseFixture.Password)),
            new Mock<IMessageBus>().Object);

        Extensions.AddDapperTypeHandlers();

        _sut = new ThreadStreamingProvider(Log.Logger, _repository);
        _utcNow = _fixture.Create<DateTimeOffset>();
    }

    public void Dispose()
    {
        _databaseFixture?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task ShouldReturnEmpty_On_No_Threads()
    {
        var result = await _sut.GetAllCheckpoints(TestContext.Current.CancellationToken);
        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldReturn_Correct_Checkpoints()
    {
        var t1 = Thread.NewThread(_utcNow);
        await _repository.SaveThread(t1, TestContext.Current.CancellationToken);

        var t2 = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        await _repository.SaveThread(t2, TestContext.Current.CancellationToken);

        var t3 = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        t3.AddLlmMessage(_fixture.Create<string>(), t3.Messages[0], _utcNow);
        await _repository.SaveThread(t3, TestContext.Current.CancellationToken);

        var t4 = Thread.NewThread(_utcNow).AddUserMessage(_fixture.Create<string>(), _utcNow);
        await _repository.SaveThread(t4, TestContext.Current.CancellationToken);
        await _repository.DeleteThread(t4.Id, TestContext.Current.CancellationToken);

        var result = await _sut.GetAllCheckpoints(TestContext.Current.CancellationToken);
        result.Count.ShouldBe(3);
        result[t1.Id].ShouldBe(0);
        result[t2.Id].ShouldBe(1);
        result[t3.Id].ShouldBe(2);
    }
}