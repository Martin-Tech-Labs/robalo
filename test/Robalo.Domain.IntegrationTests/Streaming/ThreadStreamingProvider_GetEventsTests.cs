using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Streaming;
using Serilog;

namespace Robalo.Domain.IntegrationTests.Streaming;

public class ThreadStreamingProvider_GetEventsTests : IClassFixture<DatabaseFixture>
{
    readonly ThreadStreamingProvider _sut;
    readonly SqliteThreadRepository _repository;
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public ThreadStreamingProvider_GetEventsTests(DatabaseFixture fixture)
    {
        _repository = new SqliteThreadRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new(fixture.Database, fixture.Password)),
            new Mock<IMessageBus>().Object);

        _sut = new ThreadStreamingProvider(Log.Logger, _repository);

        Extensions.AddDapperTypeHandlers();
    }

    [Fact]
    public async Task ShouldReturnEmpty_OnNonExistingThread()
    {
        var res = await _sut.GetEvents(ThreadIdentifier.NewIdentifier(), None.Default, TestContext.Current.CancellationToken).ToListAsync(TestContext.Current.CancellationToken);

        res.ShouldNotBeNull();
        res.ShouldBeEmpty();
    }
}