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
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using Xunit.Internal;

namespace Robalo.Controller.UnitTests.Controllers;

public class ServiceDiscoveryController_GetServiceDiscovery
{
    readonly ServiceDiscoveryController _sut;
    Mock<IHttpContextAccessor> _httpContextAccessor = new();
    Mock<HttpContext> _httpContext = new();
    readonly string _scheme;
    readonly string _host;
    readonly int _port;
    readonly string _pathBase;

    readonly Fixture _fixture = new();

    public ServiceDiscoveryController_GetServiceDiscovery()
    {
        _sut = new(
          logger: Log.Logger,
          httpContextAccessor: _httpContextAccessor.Object);

        _httpContextAccessor.Setup(h => h.HttpContext).Returns(_httpContext.Object);

        _scheme = $"test{_fixture.Create<Guid>().ToString("N")}";
        _host = _fixture.Create<string>();
        _port = _fixture.Create<int>();
        _pathBase = _fixture.Create<string>();

        _httpContext.Setup(h => h.Request.Scheme).Returns(_scheme);
        _httpContext.Setup(h => h.Request.Host).Returns(new HostString(host: _host, _port));
        _httpContext.Setup(h => h.Request.PathBase).Returns($"/{_pathBase}");
    }

    [Fact]
    public async Task ShouldReturnOk_WithLinks()
    {
        var result = await _sut.GetServiceDiscovery(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        okResult.Value.ShouldBeOfType<ServiceDiscoveryResource>();

        var serviceDiscovery = (ServiceDiscoveryResource)okResult.Value;

        serviceDiscovery.ValidateAllNotNullLinks(_scheme, _host, _port, _pathBase);
    }
}