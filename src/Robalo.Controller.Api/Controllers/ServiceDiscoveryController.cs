using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Controller.Api.Hypermedia;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("")]
public class ServiceDiscoveryController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;


    public ServiceDiscoveryController(
        ILogger logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger.ForContext(GetType());
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public async Task<IActionResult> GetServiceDiscovery(CancellationToken cancellationToken) => 
        new ServiceDiscoveryResource(
            Links: new(
                Self: _httpContextAccessor.LinkToServiceDiscovery(),
                Threads: _httpContextAccessor.LinkToThreads(),
                CreateThread: _httpContextAccessor.LinkToThreads(),
                Events: _httpContextAccessor.LinkToEvents())
        ).AsOk();
}