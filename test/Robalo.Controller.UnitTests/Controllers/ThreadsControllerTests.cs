using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Extensions;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Controller.UnitTests.Controllers;

public abstract class ThreadsControllerTests
{
    internal readonly ThreadsController _sut;
    internal readonly Mock<IDateTimeOffsetProvider> _dateTimeOffsetProvider = new();
    internal readonly Mock<IThreadRepository> _repository = new();
    internal readonly Mock<ProblemDetailsFactory> _problemDetailsFactory = new();
    internal readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
    internal readonly Mock<HttpContext> _httpContext = new();

    internal readonly string _scheme;
    internal readonly string _pathBase;
    internal readonly string _host;
    internal readonly int _port;

    internal readonly Fixture _fixture = new();
    internal readonly DateTimeOffset _utcNow;

    public ThreadsControllerTests()
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

    protected void ValidateLink(params Link?[] links)
    {
        foreach (var link in links)
        {
            link.ShouldNotBeNull();
            link.Templated.ShouldBeFalse();
            link.Href.ShouldNotBeNull();
            link.Href.Scheme.ShouldBe(_scheme);
            link.Href.Host.ShouldBe(_host);
            link.Href.Port.ShouldBe(_port);
            link.Href.Segments[1].ShouldBe($"{_pathBase}/");
        }
    }

    protected void ValidateAllNotNullLinks<TLinks>(HalResource<TLinks> resource) where TLinks : Links
    {
        resource.Links.ShouldNotBeNull();

        var context = new NullabilityInfoContext();

        var links = resource.Links
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(Link) && context.Create(p).ReadState == NullabilityState.NotNull)
            .Select(p => p.GetValue(resource.Links))
            .ToList();

        if (links is null || links.Count == 0)
        {
            throw new InvalidOperationException("No links found on the supplied object");
        }

        foreach (Link link in links!)
        {
            link.ShouldNotBeNull();
            link.Templated.ShouldBeFalse();
            link.Href.ShouldNotBeNull();
            link.Href.Scheme.ShouldBe(_scheme);
            link.Href.Host.ShouldBe(_host);
            link.Href.Port.ShouldBe(_port);
            link.Href.Segments[1].ShouldBe($"{_pathBase}/");
        }
    }
}