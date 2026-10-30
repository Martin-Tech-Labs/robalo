using Microsoft.AspNetCore.Mvc;
using Robalo.Common.Streaming;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    readonly ILogger _logger;
    readonly IThreadRepository _repository;
    readonly IStreamingService<ThreadIdentifier, Message, int> _streamingService;

    public EventsController(
        ILogger logger,
        IThreadRepository repository,
        IStreamingService<ThreadIdentifier, Message, int> streamingService)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;
        _streamingService = streamingService;
    }

    [HttpGet("")]
    public IResult Get(CancellationToken cancellationToken)
    {
        var result = _streamingService.Pull(_ => true, cancellationToken);
        return Results.ServerSentEvents(result);

    }
}