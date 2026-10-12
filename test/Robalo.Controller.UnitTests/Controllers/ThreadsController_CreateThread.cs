using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Events;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public class ThreadsController_CreateThread
{
    readonly ThreadsController _sut;
    readonly Mock<IDateTimeOffsetProvider> _dateTimeOffsetProvider = new();
    readonly Mock<IThreadRepository> _repository = new();
    readonly Mock<ProblemDetailsFactory> _problemDetailsFactory = new();
    readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    readonly Mock<HttpContext> _httpContext = new();

    readonly string _scheme;
    readonly string _pathBase;
    readonly string _host;
    readonly int _port;

    readonly Fixture _fixture = new();
    static DateTimeOffset _utcNow;

    public ThreadsController_CreateThread()
    {
        _sut = new(
            logger: Log.Logger,
            httpContextAccessor: _httpContextAccessor.Object,
            threadRepository: _repository.Object,
            problemDetailsFactory: _problemDetailsFactory.Object,
            dateTimeOffsetProvider: _dateTimeOffsetProvider.Object);

        _problemDetailsFactory.Setup(
            p => p.CreateProblemDetails(
                It.IsAny<HttpContext>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
            .Returns(_fixture.Create<ProblemDetails>());

        _httpContextAccessor.Setup(h => h.HttpContext).Returns(_httpContext.Object);

        _scheme = $"test{_fixture.Create<Guid>().ToString("N")}";
        _host = _fixture.Create<string>();
        _port = _fixture.Create<int>();
        _pathBase = _fixture.Create<string>();

        _httpContext.Setup(h => h.Request.Scheme).Returns(_scheme);
        _httpContext.Setup(h => h.Request.Host).Returns(new HostString(host: _host, _port));
        _httpContext.Setup(h => h.Request.PathBase).Returns($"/{_pathBase}");

        _utcNow = _fixture.Create<DateTimeOffset>();
        _dateTimeOffsetProvider.Setup(d => d.UtcNow).Returns(_utcNow);
    }


    [Fact]
    public async Task ShouldReturnCreated_OnCreatedThread()
    {
        _repository.Invocations.Clear();

        var request = _fixture.Create<MessageApiRequest>();

        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Created());

        var result = await _sut.CreateThread(
            request: request,
            cancellationToken: TestContext.Current.CancellationToken);

        result.ShouldBeOfType<CreatedResult>();
        var createdResult = (CreatedResult)result;

        createdResult.Value.ShouldBeOfType<ThreadResource>();

        var resource = (ThreadResource)createdResult.Value;

        resource.Links.ShouldNotBeNull();
        resource.Links.Self.ShouldNotBeNull();
        resource.Links.Self.Href.ShouldNotBeNull();
        resource.Links.Self.Href.Scheme.ShouldBe(_scheme);
        resource.Links.Self.Href.Host.ShouldBe(_host);
        resource.Links.Self.Href.Port.ShouldBe(_port);
        resource.Links.Self.Href.Segments[1].ShouldBe($"{_pathBase}/");


        resource.Links.Events.ShouldNotBeNull();
        resource.Links.Events.Href.ShouldNotBeNull();
        resource.Links.Events.Href.Scheme.ShouldBe(_scheme);
        resource.Links.Events.Href.Host.ShouldBe(_host);
        resource.Links.Events.Href.Port.ShouldBe(_port);
        resource.Links.Events.Href.Segments[1].ShouldBe($"{_pathBase}/");

        resource.Links.Messages.ShouldNotBeNull();
        resource.Links.Messages.Href.ShouldNotBeNull();
        resource.Links.Messages.Href.Scheme.ShouldBe(_scheme);
        resource.Links.Messages.Href.Host.ShouldBe(_host);
        resource.Links.Messages.Href.Port.ShouldBe(_port);
        resource.Links.Messages.Href.Segments[1].ShouldBe($"{_pathBase}/");

        createdResult.Location.ShouldBe(resource.Links.Self.Href.AbsoluteUri);
        resource.CreatedOn.ShouldBe(_utcNow);
        resource.ModifiedOn.ShouldBe(_utcNow);
        resource.Title.ShouldBeNullOrWhiteSpace();
        resource.ThreadId.ShouldNotBeNullOrWhiteSpace();

        _repository
            .Verify(r => r.SaveThread(It.Is<Thread>(
                t => t.CreatedOn == _utcNow &&
                t.Messages.Count == 1 &&
                t.Title == null &&
                t.Messages[0].Content == request.Content &&
                t.Messages[0].CreatedOn == _utcNow &&
                t.Id == resource.ThreadId &&
                t.PendingEvents.Count == 1 &&
                t.PendingEvents[0].GetType() == typeof(UserMessageAdded) &&
                ((UserMessageAdded)t.PendingEvents[0]).Content == request.Content &&
                ((UserMessageAdded)t.PendingEvents[0]).AddedOn == _utcNow), TestContext.Current.CancellationToken),
            Times.Once());
    }

    [Fact]
    public async Task ShouldReturnConflict_OnConcurrencyConflict()
    {
        _problemDetailsFactory.Invocations.Clear();

        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ConcurrencyConflict());

        var result = await _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken);

        result.ShouldBeOfType<ConflictObjectResult>();
        var conflictResult = (ConflictObjectResult)result;

        conflictResult.Value.ShouldNotBeNull();
        conflictResult.Value.ShouldBeOfType<ProblemDetails>();

        _problemDetailsFactory.Verify(v => v.CreateProblemDetails(
            It.IsAny<HttpContext>(), 409, "Conflict", It.IsAny<string>(), "thread_already_exists", It.IsAny<string>()), Times.Once());
    }

    [Fact]
    public async Task ShouldThrow_OnRepositoryUpdatedResult()
    {
        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Updated());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ShouldThrow_OnRepositoryNoChangesResult()
    {
        _repository
            .Setup(r => r.SaveThread(It.IsAny<Thread>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NoChanges());

        await Should.ThrowAsync<InvalidOperationException>(() => _sut.CreateThread(
            _fixture.Create<MessageApiRequest>(),
            TestContext.Current.CancellationToken));
    }
}