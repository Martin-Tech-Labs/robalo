using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Common.Streaming;
using Robalo.Tests.Common;
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

        _streamingProvider.Verify(s => s.GetAllCheckpoints(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_IgnoreAlreadyReplayedEvents()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

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

        var nextEvent = enumerator.MoveNextAsync();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());

        _streamingProvider.Verify(s => s.GetAllCheckpoints(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_CatchUpOnLastEvents()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

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

        var nextEvent = enumerator.MoveNextAsync();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());

        _streamingProvider.Verify(s => s.GetAllCheckpoints(It.IsAny<CancellationToken>()), Times.Once());

    }

    [Fact]
    async Task Pull_WithCheckpoint_Should_CatchUpOnLastEvents_WithLock()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

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

        var nextEvent = enumerator.MoveNextAsync();

        await _sut.PushForStream(streamId, TestContext.Current.CancellationToken);

        await nextEvent;
        result.Add(enumerator.Current);

        result.ShouldBeEquivalentTo(streamingEvents[(checkpointAsIndex + 1)..].ToList());

        _streamingProvider.Verify(s => s.GetAllCheckpoints(It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    async Task PushForStream_ShouldCorrectlyDeliverByStreamId()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var streamId1 = _fixture.Create<string>();
        var streamId2 = _fixture.Create<string>();

        var events1 = _fixture.CreateMany<TestEvent>(4);
        var events2 = _fixture.CreateMany<TestEvent>(4);

        var i = 0;
        var streamingEvents1 = events1.Select(e => new StreamingEvent<string, TestEvent, int>(streamId1, e, ++i)).ToList();
        i = 0;
        var streamingEvents2 = events2.Select(e => new StreamingEvent<string, TestEvent, int>(streamId2, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId1, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents1.ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId2, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents2.ToAsyncEnumerable());


        await using var enumerator1 = _sut
            .Pull(streamId1, None.Default, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator2 = _sut
            .Pull(streamId2, None.Default, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);


        var result1 = new List<StreamingEvent<string, TestEvent, int>>();
        var result2 = new List<StreamingEvent<string, TestEvent, int>>();

        var nextEvent1 = enumerator1.MoveNextAsync();
        var nextEvent2 = enumerator2.MoveNextAsync();

        var task1 = _sut.PushForStream(streamId1, TestContext.Current.CancellationToken);
        var task2 = _sut.PushForStream(streamId2, TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        await nextEvent1;
        result1.Add(enumerator1.Current);
        await nextEvent2;
        result2.Add(enumerator2.Current);

        for (var j = 1; j <= 3; j++)
        {
            await enumerator1.MoveNextAsync();
            result1.Add(enumerator1.Current);
            await enumerator2.MoveNextAsync();
            result2.Add(enumerator2.Current);
        }


        result1.ShouldBeEquivalentTo(streamingEvents1);
        result2.ShouldBeEquivalentTo(streamingEvents2);
    }

    [Fact]
    async Task PushForStream_ShouldCorrectlyDeliverByStreamId_When_Using_Pull_WithGlobalFilter()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var streamId1 = _fixture.Create<string>();
        var streamId2 = _fixture.Create<string>();

        var events1 = _fixture.CreateMany<TestEvent>(4);
        var events2 = _fixture.CreateMany<TestEvent>(4);

        var i = 0;
        var streamingEvents1 = events1.Select(e => new StreamingEvent<string, TestEvent, int>(streamId1, e, ++i)).ToList();
        i = 0;
        var streamingEvents2 = events2.Select(e => new StreamingEvent<string, TestEvent, int>(streamId2, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId1, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents1.ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId2, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents2.ToAsyncEnumerable());


        await using var enumerator1 = _sut
            .Pull(e => e.StreamId == streamId1, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator2 = _sut
            .Pull(e => e.StreamId == streamId2, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);


        var result1 = new List<StreamingEvent<string, TestEvent, int>>();
        var result2 = new List<StreamingEvent<string, TestEvent, int>>();

        var nextEvent1 = enumerator1.MoveNextAsync();
        var nextEvent2 = enumerator2.MoveNextAsync();

        var task1 = _sut.PushForStream(streamId1, TestContext.Current.CancellationToken);
        var task2 = _sut.PushForStream(streamId2, TestContext.Current.CancellationToken);

        await Task.WhenAll(task1, task2);

        await nextEvent1;
        result1.Add(enumerator1.Current);
        await nextEvent2;
        result2.Add(enumerator2.Current);

        for (var j = 1; j <= 3; j++)
        {
            await enumerator1.MoveNextAsync();
            result1.Add(enumerator1.Current);
            await enumerator2.MoveNextAsync();
            result2.Add(enumerator2.Current);
        }


        result1.ShouldBeEquivalentTo(streamingEvents1);
        result2.ShouldBeEquivalentTo(streamingEvents2);
    }

    [Fact]
    async Task PushForStream_ShouldCorrectlyDeliverByStreamId_WithCorrectLiveCheckpoints()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var streamId1 = _fixture.Create<string>();
        var streamId2 = _fixture.Create<string>();

        var events1 = _fixture.CreateMany<TestEvent>(4);
        var events2 = _fixture.CreateMany<TestEvent>(4);

        var i = 0;
        var streamingEvents1 = events1.Select(e => new StreamingEvent<string, TestEvent, int>(streamId1, e, ++i)).ToList();
        i = 0;
        var streamingEvents2 = events2.Select(e => new StreamingEvent<string, TestEvent, int>(streamId2, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId1, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents1[..^1].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId1, 3, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents1[^1..].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId2, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents2[..^1].ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId2, 3, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents2[^1..].ToAsyncEnumerable());

        await using var enumerator1 = _sut
            .Pull(streamId1, None.Default, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator2 = _sut
            .Pull(streamId2, None.Default, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);


        var result1 = new List<StreamingEvent<string, TestEvent, int>>();
        var result2 = new List<StreamingEvent<string, TestEvent, int>>();

        var nextEvent1 = enumerator1.MoveNextAsync();
        var nextEvent2 = enumerator2.MoveNextAsync();

        await Task.WhenAll([
            _sut.PushForStream(streamId1, TestContext.Current.CancellationToken),
            _sut.PushForStream(streamId2, TestContext.Current.CancellationToken),
            _sut.PushForStream(streamId2, TestContext.Current.CancellationToken),
            _sut.PushForStream(streamId1, TestContext.Current.CancellationToken)]);

        await nextEvent1;
        result1.Add(enumerator1.Current);
        await nextEvent2;
        result2.Add(enumerator2.Current);

        for (var j = 1; j <= 3; j++)
        {
            await enumerator1.MoveNextAsync();
            result1.Add(enumerator1.Current);
            await enumerator2.MoveNextAsync();
            result2.Add(enumerator2.Current);
        }

        result1.ShouldBeEquivalentTo(streamingEvents1);
        result2.ShouldBeEquivalentTo(streamingEvents2);
    }

    [Fact]
    async Task PushForStream_With_Filter_ShouldDeliverCorrectEvents()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(2));

        var streamId1 = _fixture.Create<string>();
        var streamId2 = _fixture.Create<string>();

        var events11 = new List<TestEvent> {
            new (_fixture.Create<string>(), 11),
            new (_fixture.Create<string>(), 11)};

        var events12 = new List<TestEvent> {
            new (_fixture.Create<string>(), 12),
            new (_fixture.Create<string>(), 12)};

        var events21 = new List<TestEvent> {
            new (_fixture.Create<string>(), 21),
            new (_fixture.Create<string>(), 21)};

        var events22 = new List<TestEvent> {
            new (_fixture.Create<string>(), 22),
            new (_fixture.Create<string>(), 22)};

        var i = 0;
        var streamingEvents11 = events11.Select(e => new StreamingEvent<string, TestEvent, int>(streamId1, e, ++i)).ToList();
        var streamingEvents12 = events12.Select(e => new StreamingEvent<string, TestEvent, int>(streamId1, e, ++i)).ToList();
        i = 0;
        var streamingEvents21 = events21.Select(e => new StreamingEvent<string, TestEvent, int>(streamId2, e, ++i)).ToList();
        var streamingEvents22 = events22.Select(e => new StreamingEvent<string, TestEvent, int>(streamId2, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId1, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents11.Concat(streamingEvents12).ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId2, It.IsNone<int>(), It.IsAny<CancellationToken>()))
            .Returns(streamingEvents21.Concat(streamingEvents22).ToAsyncEnumerable());

        await using var enumerator11 = _sut
            .Pull(streamId1, None.Default, e => e.Day == 11, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator12 = _sut
            .Pull(streamId1, None.Default, e => e.Day == 12, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator21 = _sut
            .Pull(streamId2, None.Default, e => e.Day == 21, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        await using var enumerator22 = _sut
            .Pull(streamId2, None.Default, e => e.Day == 22, timeout.Token)
            .GetAsyncEnumerator(timeout.Token);

        var result11 = new List<StreamingEvent<string, TestEvent, int>>();
        var result12 = new List<StreamingEvent<string, TestEvent, int>>();
        var result21 = new List<StreamingEvent<string, TestEvent, int>>();
        var result22 = new List<StreamingEvent<string, TestEvent, int>>();

        var nextEvent11 = enumerator11.MoveNextAsync();
        var nextEvent12 = enumerator12.MoveNextAsync();
        var nextEvent21 = enumerator21.MoveNextAsync();
        var nextEvent22 = enumerator22.MoveNextAsync();

        await Task.WhenAll([
            _sut.PushForStream(streamId1, TestContext.Current.CancellationToken),
            _sut.PushForStream(streamId2, TestContext.Current.CancellationToken)]);

        await nextEvent11;
        result11.Add(enumerator11.Current);
        await nextEvent12;
        result12.Add(enumerator12.Current);
        await nextEvent21;
        result21.Add(enumerator21.Current);
        await nextEvent22;
        result22.Add(enumerator22.Current);

        for (var j = 1; j <= 1; j++)
        {
            await enumerator11.MoveNextAsync();
            result11.Add(enumerator11.Current);

            await enumerator12.MoveNextAsync();
            result12.Add(enumerator12.Current);

            await enumerator21.MoveNextAsync();
            result21.Add(enumerator21.Current);

            await enumerator22.MoveNextAsync();
            result22.Add(enumerator22.Current);
        }

        result11.ShouldBeEquivalentTo(streamingEvents11);
        result12.ShouldBeEquivalentTo(streamingEvents12);
        result21.ShouldBeEquivalentTo(streamingEvents21);
        result22.ShouldBeEquivalentTo(streamingEvents22);
    }

    public void Dispose()
    {
        _sut?.Dispose();
        GC.SuppressFinalize(this);
    }
}