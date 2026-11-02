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

    [Fact]
    async Task Pull_ShouldReplay_Events()
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(200));

        var streamId = _fixture.Create<string>();
        var checkpoint = _fixture.Create<int>();

        var events = _fixture.CreateMany<TestEvent>(_fixture.Create<int>());
        var i = 0;
        var streamingEvents = events.Select(e => new StreamingEvent<string, TestEvent, int>(streamId, e, ++i)).ToList();

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, checkpoint, It.IsAny<CancellationToken>()))
            .Returns(streamingEvents.ToAsyncEnumerable());

        _streamingProvider
            .Setup(s => s.GetEvents(streamId, It.Is<Option<int>>(e => e != checkpoint), It.IsAny<CancellationToken>()))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<string, TestEvent, int>>());

        var result = await _sut.Pull(streamId, checkpoint, timeout.Token)
            .Take(events.Count())
            .ToListAsync(timeout.Token);

        result.ShouldNotBeNull();
        result.ShouldBeEquivalentTo(streamingEvents.ToList());
    }


    public void Dispose()
    {
        _sut?.Dispose();
        GC.SuppressFinalize(this);
    }
}