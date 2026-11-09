using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Controllers;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Configuration;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using Xunit.Internal;

namespace Robalo.Controller.UnitTests.Controllers;

public class ConfigurationController_GetTests
{
    readonly ConfigurationController _sut;
    Mock<IHttpContextAccessor> _httpContextAccessor = new();
    Mock<HttpContext> _httpContext = new();
    Mock<IConfigurationRepository> _repository = new();
    readonly string _scheme;
    readonly string _host;
    readonly int _port;
    readonly string _pathBase;

    readonly Fixture _fixture = new();

    public ConfigurationController_GetTests()
    {
        _sut = new(
          logger: Log.Logger,
          repository: _repository.Object,
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
    public async Task OnMissingConfiguraiton_ShouldReturnNotFound()
    {
        _repository.Setup(r => r.LoadConfiguration(TestContext.Current.CancellationToken)).ReturnsAsync(None.Default);
        var result = await _sut.Get(TestContext.Current.CancellationToken);
        result.ShouldBeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task OnExistingConfiguration_ShouldReturnOk_WithLinks()
    {
        var robaloConfig = _fixture.Create<RobaloConfiguration>();
        robaloConfig.Value.Verify();
        _repository.Setup(r => r.LoadConfiguration(TestContext.Current.CancellationToken)).ReturnsAsync(robaloConfig);
        var result = await _sut.Get(TestContext.Current.CancellationToken);

        result.ShouldBeOfType<OkObjectResult>();
        var okResult = (OkObjectResult)result;

        okResult.Value.ShouldBeOfType<ConfigurationResource>();

        var serviceDiscovery = (ConfigurationResource)okResult.Value;

        serviceDiscovery.ValidateAllNotNullLinks(_scheme, _host, _port, _pathBase);
        serviceDiscovery.ConfigurationId.ShouldBe(robaloConfig.ConfigurationId);
        serviceDiscovery.CreatedOn.ShouldBe(robaloConfig.CreatedOn);
        serviceDiscovery.Data.ShouldNotBeNull();
        serviceDiscovery.Data.LlmOptions.ShouldNotBeNull();

        serviceDiscovery.Data.LlmOptions.Endpoint.ShouldBe(robaloConfig.Value.LlmOptions!.Endpoint);
        serviceDiscovery.Data.LlmOptions.Model.ShouldBe(robaloConfig.Value.LlmOptions!.Model);
        serviceDiscovery.Data.LlmOptions.Key.ShouldNotBe(robaloConfig.Value.LlmOptions!.Key);
        serviceDiscovery.Data.LlmOptions.Key.ShouldBe("<hidden>");
    }
}