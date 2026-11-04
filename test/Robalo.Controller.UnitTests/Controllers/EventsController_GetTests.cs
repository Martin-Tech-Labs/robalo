using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Common.Streaming;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Tests.Common;
using Serilog;
using Xunit.Internal;
using HttpResults = Microsoft.AspNetCore.Http.HttpResults;

namespace Robalo.Controller.UnitTests.Controllers;

public class EventsController_GetTests
{
    readonly EventsController _sut;
    Mock<IHttpContextAccessor> _httpContextAccessor = new();
    Mock<HttpContext> _httpContext = new();
    Mock<IThreadRepository> _repository = new();
    Mock<IStreamingService<ThreadIdentifier, Message, int>> _streamingService = new();
    readonly string _scheme;
    readonly string _host;
    readonly int _port;
    readonly string _pathBase;

    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;

    public EventsController_GetTests()
    {
        _sut = new(
          logger: Log.Logger,
          _repository.Object,
          _streamingService.Object,
          httpContextAccessor: _httpContextAccessor.Object);

        _httpContextAccessor.Setup(h => h.HttpContext).Returns(_httpContext.Object);

        _scheme = $"test{_fixture.Create<Guid>().ToString("N")}";
        _host = _fixture.Create<string>();
        _port = _fixture.Create<int>();
        _pathBase = _fixture.Create<string>();

        _httpContext.Setup(h => h.Request.Scheme).Returns(_scheme);
        _httpContext.Setup(h => h.Request.Host).Returns(new HostString(host: _host, _port));
        _httpContext.Setup(h => h.Request.PathBase).Returns($"/{_pathBase}");

        _utcNow = _fixture.Create<DateTimeOffset>();
    }

    [Fact]
    public async Task ShouldSubscribe_WithNoThreadAndCheckpoint()
    {
        _streamingService
            .Setup(s => s.PullAll(TestContext.Current.CancellationToken))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<ThreadIdentifier, Message, int>>());

        var result = await _sut.Get(default, default, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.ServerSentEventsResult<MessageResource>>();

        _repository.VerifyNoOtherCalls();
        _streamingService.Verify(s => s.PullAll(TestContext.Current.CancellationToken), Times.Once());
        _streamingService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldSubscribe_WithThreadAndNoCheckpoint()
    {
        var thread = Thread.NewThread(_utcNow);

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _streamingService
            .Setup(s => s.Pull(thread.Id, It.IsNone<int>(), TestContext.Current.CancellationToken))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<ThreadIdentifier, Message, int>>());

        var result = await _sut.Get(thread.Id, default, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.ServerSentEventsResult<MessageResource>>();

        _streamingService.Verify(s => s.Pull(thread.Id, It.IsNone<int>(), TestContext.Current.CancellationToken), Times.Once());
        _streamingService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task ShouldSubscribe_WithThreadAndCheckpoint(int checkpoint)
    {
        var thread = Thread.NewThread(_utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .AddUserMessage(_fixture.Create<string>(), _utcNow)
            .MarkAggregateAsSynchronized();

        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        _streamingService
            .Setup(s => s.Pull(thread.Id, checkpoint, TestContext.Current.CancellationToken))
            .Returns(AsyncEnumerable.Empty<StreamingEvent<ThreadIdentifier, Message, int>>());

        var result = await _sut.Get(thread.Id, checkpoint, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.ServerSentEventsResult<MessageResource>>();

        _streamingService.Verify(s => s.Pull(thread.Id, checkpoint, TestContext.Current.CancellationToken), Times.Once());
        _streamingService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnNonExistingThread()
    {

        var threadId = ThreadIdentifier.NewIdentifier();
        _repository.Setup(r => r.GetThread(threadId, TestContext.Current.CancellationToken));


        var result = await _sut.Get(threadId, default, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.NotFound>();
        _repository.Verify(r => r.GetThread(threadId, TestContext.Current.CancellationToken), Times.Once());
        _streamingService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnInvalidThreadId()
    {
        var result = await _sut.Get(_fixture.Create<string>(), default, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.NotFound>();
        _repository.VerifyNoOtherCalls();
        _streamingService.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ShouldReturnNotFound_OnCheckpointWithoutThreadId()
    {
        var result = await _sut.Get(default, _fixture.Create<int>(), TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.NotFound>();
        _repository.VerifyNoOtherCalls();
        _streamingService.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(4)]
    public async Task ShouldReturnNotFound_OnInvalidCheckpoint(int checkpoint)
    {
        var thread = Thread.NewThread(_utcNow) // 0
            .AddUserMessage(_fixture.Create<string>(), _utcNow) // 1
            .AddUserMessage(_fixture.Create<string>(), _utcNow) // 2
            .AddUserMessage(_fixture.Create<string>(), _utcNow) // 3
            .MarkAggregateAsSynchronized();
        _repository
            .Setup(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken))
            .ReturnsAsync(thread);

        var result = await _sut.Get(thread.Id, checkpoint, TestContext.Current.CancellationToken);

        result.ShouldBeOfType<HttpResults.NotFound>();
        _repository.Verify(r => r.GetThread(thread.Id, TestContext.Current.CancellationToken), Times.Once());
        _streamingService.VerifyNoOtherCalls();
    }
}