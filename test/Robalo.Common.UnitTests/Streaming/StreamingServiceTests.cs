using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Streaming;
using Serilog;

namespace Robalo.Common.UnitTests.Streaming;

public class StreamingServiceTests : IDisposable
{
    public sealed record TestEvent(string Name, int Day);
    readonly StreamingService<string, TestEvent, int> _sut;
    readonly Mock<IStreamingProvider<string, TestEvent, int>> _streamingProvider = new();
    readonly Fixture _fixture = new();

    public StreamingServiceTests()
    {
        _sut = new(Log.Logger, _streamingProvider.Object);
        _streamingProvider
            .Setup(s => s.GetAllCheckpoints(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<string, int>());
    }

    [Fact]
    void Pull_ShouldThrow_IfCancellationTokenIsNotCancellable()
    {
        Should.ThrowAsync<ArgumentException>(async () => await _sut.PullAll(CancellationToken.None).ToListAsync());
    }

    async Task Pull_Should_InitializeCheckpoint_FirstTimeItCalled()
    {
        _sut.PullAll(TestContext.Current.CancellationToken);

        _streamingProvider.Verify(s => s.GetAllCheckpoints(TestContext.Current.CancellationToken), Times.Once());
        _sut.PullAll(TestContext.Current.CancellationToken);

        _streamingProvider.VerifyNoOtherCalls();
    }

    [Fact]
    async Task Pull_WithCheckpoint_ShouldReplay_Events()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var streamId = _fixture.Create<string>();
        var checkpoint = _fixture.Create<int>();

        var events = _fixture.CreateMany<TestEvent>(_fixture.Create<int>());
        var i = 0;
        var streamingEvents = events.Select(e => new StreamingEvent<string, TestEvent, int>(streamId, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, checkpoint, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents.ToAsyncEnumerable());


        var result = await _sut.Pull(streamId, checkpoint, timeout.Token)
            .Take(events.Count())
            .ToListAsync(timeout.Token);

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(streamingEvents.ToList());
    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_IgnoreAlreadyReplayedEvents()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(200));

        var streamId = _fixture.Create<string>();
        var checkpoint = 1;
        var checkpointAsIndex = checkpoint - 1;

        var events = _fixture.CreateMany<TestEvent>(6);
        var i = 0;
        var streamingEvents = events.Select(e => new StreamingEvent<string, TestEvent, int>(streamId, e, ++i)).ToList();
        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 1, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[(checkpointAsIndex + 1)..^1].ToAsyncEnumerable()); // from checkpoint till penunlimate item

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 5, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<string, TestEvent, int>>());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, It.Is<Option<int>>(t => t.IsNone), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents.ToAsyncEnumerable()); // from beginning till last item

        await using var enumerator = _sut
            .Pull(streamId, checkpoint, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var result = new List<StreamingEvent<string, TestEvent, int>>();

        for (var j = 0; j < 4; j++)
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            result.Add(enumerator.Current);
        }

        var nextEvent = enumerator.MoveNextAsync().AsTask();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());
    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_CatchUpOnLastEvents()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(200));

        var streamId = _fixture.Create<string>();
        var checkpoint = 1;
        var checkpointAsIndex = checkpoint - 1;

        var events = _fixture.CreateMany<TestEvent>(7);
        var i = 0;
        var streamingEvents = events.Select(e => new StreamingEvent<string, TestEvent, int>(streamId, e, ++i)).ToList();
        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 1, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[(checkpointAsIndex + 1)..^2].ToAsyncEnumerable()); // from checkpoint till penunlimate item

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 5, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[^2..^1].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 6, It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<string, TestEvent, int>>());


        _streamingProvider
            .Setup(s => s.GetEvents(streamId, It.Is<Option<int>>(t => t.IsNone), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents.ToAsyncEnumerable()); // from beginning till last item

        await using var enumerator = _sut
            .Pull(streamId, checkpoint, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var result = new List<StreamingEvent<string, TestEvent, int>>();

        for (var j = 0; j < 5; j++)
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            result.Add(enumerator.Current);
        }

        var nextEvent = enumerator.MoveNextAsync().AsTask();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());
    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_CatchUpOnLastEvents_WithLock()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(200));

        var streamId = _fixture.Create<string>();
        var checkpoint = 1;
        var checkpointAsIndex = checkpoint - 1;

        var events = _fixture.CreateMany<TestEvent>(8);
        var i = 0;
        var streamingEvents = events.Select(e => new StreamingEvent<string, TestEvent, int>(streamId, e, ++i)).ToList();
        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 1, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[(checkpointAsIndex + 1)..^3].ToAsyncEnumerable()); // from checkpoint till penunlimate item

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 5, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[^3..^2].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, 6, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents[^2..^1].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, It.Is<Option<int>>(t => t.IsNone), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents.ToAsyncEnumerable()); // from beginning till last item

        await using var enumerator = _sut
            .Pull(streamId, checkpoint, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var result = new List<StreamingEvent<string, TestEvent, int>>();

        for (var j = 0; j < 6; j++)
        {
            (await enumerator.MoveNextAsync()).ShouldBeTrue();
            result.Add(enumerator.Current);
        }

        var nextEvent = enumerator.MoveNextAsync().AsTask();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());
    }

    public void Dispose()
    {
        _sut?.Dispose();
        GC.SuppressFinalize(this);
    }
}