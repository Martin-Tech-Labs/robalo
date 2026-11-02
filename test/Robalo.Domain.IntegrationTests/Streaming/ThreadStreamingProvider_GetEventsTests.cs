using Moq;
using Robalo.Common.Coordination;
using Robalo.Common.Extensions;
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
        var res = await _sut.GetEvents(ThreadIdentifier.NewIdentifier(), None.Default, TestContext.Current.CancellationToken)
        .ToListAsync(TestContext.Current.CancellationToken);

        res.ShouldNotBeNull();
        res.ShouldBeEmpty();
    }

    [Fact]
    public async Task ShouldReturnEmpty_OnThreadWithNoEvents()
    {
        var thread = Thread.NewThread(_utcNow);
        await _repository.SaveThread(thread, TestContext.Current.CancellationToken);

        var res = await _sut.GetEvents(thread.Id, None.Default, TestContext.Current.CancellationToken)
        .ToListAsync(TestContext.Current.CancellationToken);

        res.ShouldNotBeNull();
        res.ShouldBeEmpty();
    }


    [Theory]
    [InlineData(-1, 100, 1, 100)]
    [InlineData(0, 100, 1, 100)]
    [InlineData(default, 100, 1, 100)]
    [InlineData(1, 99, 2, 100)]
    [InlineData(2, 98, 3, 100)]
    [InlineData(99, 1, 100, 100)]
    [InlineData(100, 0, 0, 0)]
    [InlineData(101, 0, 0, 0)]
    public async Task ShouldReturnCorrectEvents(int? checkpoint, int numEvents, int firstNumber, int lastNumber)
    {
        var count = 100;

        var thread = Thread.NewThread(_utcNow);
        for (var i = 0; i < count; i++)
        {
            thread.AddUserMessage(_fixture.Create<string>(), thread.ModifiedOn.AddSeconds(1));
        }

        await _repository.SaveThread(thread, TestContext.Current.CancellationToken);

        var res = await _sut.GetEvents(thread.Id, checkpoint.AsOption(), TestContext.Current.CancellationToken)
            .ToListAsync(TestContext.Current.CancellationToken);

        res.ShouldNotBeNull();
        res.Count.ShouldBe(numEvents);

        if (res.Count == 0)
        {
            return;
        }

        res.DistinctBy(r => r.Checkpoint).Count().ShouldBe(numEvents);
        res.All(r => r.StreamId == thread.Id).ShouldBeTrue();
        res.All(r => r.Checkpoint == r.Event.Number).ShouldBeTrue();

        res.First().Checkpoint.ShouldBe(firstNumber);
        res.First().Event.Number.ShouldBe(firstNumber);

        res.Last().Checkpoint.ShouldBe(lastNumber);
        res.Last().Event.Number.ShouldBe(lastNumber);

        res.Select(r => r.Event).ToList().ShouldBeEquivalentTo(thread.Messages.ToList()[(firstNumber - 1)..lastNumber]);

    }
}