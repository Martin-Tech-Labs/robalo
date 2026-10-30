using Microsoft.AspNetCore.Mvc;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    readonly ILogger _logger;
    readonly IThreadRepository _repository;

    public EventsController(ILogger logger, IThreadRepository repository)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;
    }

    // [HttpGet("")]
    // public async Task<IActionResult> Get(CancellationToken cancellationToken)
    // {
    //     return Results.ServerSentEvents()
    // }
}