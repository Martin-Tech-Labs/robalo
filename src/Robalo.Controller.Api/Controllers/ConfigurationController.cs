using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Robalo.Common.Models;
using Robalo.Controller.Api.Hypermedia;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("configuration")]
public class ConfigurationController : ControllerBase
{
    readonly ILogger _logger;
    readonly IConfigurationRepository _repository;
    readonly IHttpContextAccessor _httpContextAccessor;

    public ConfigurationController(
        ILogger logger,
        IConfigurationRepository repository,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger.ForContext(GetType());
        _httpContextAccessor = httpContextAccessor;
        _repository = repository;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var configurationOrNone = await _repository.LoadConfiguration(cancellationToken);
        if (configurationOrNone is not Some<RobaloConfiguration> configuration)
        {
            return NotFound();
        }
        
        return new ConfigurationResource(
            configuration.Value.ConfigurationId,
            configuration.Value.CreatedOn,
            configuration.Value.Value.ToSafeOptions(),
            new ConfigurationLinks(_httpContextAccessor.LinkToConfiguration()))
        .AsOk();
    }
}