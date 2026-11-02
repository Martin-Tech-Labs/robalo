using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Streaming;
using Robalo.Controller.Api.Hypermedia;
using Robalo.Controller.Api.Hypermedia.Resources;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("events")]
public class EventsController : ControllerBase
{
    readonly ILogger _logger;
    readonly IThreadRepository _repository;
    readonly IHttpContextAccessor _httpContextAccessor;
    readonly IStreamingService<ThreadIdentifier, Message, int> _streamingService;

    public EventsController(
        ILogger logger,
        IThreadRepository repository,
        IStreamingService<ThreadIdentifier, Message, int> streamingService,
        IHttpContextAccessor httpContextAccessor)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;
        _streamingService = streamingService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet("")]
    public async Task<IResult> Get(
        [FromQuery] string? threadId,
        [FromQuery] int? checkpoint,
        CancellationToken cancellationToken)
    {
        var result = (threadId is not null ? ThreadIdentifier.TryGetFromId(threadId) : None.Default) switch
        {
            Some<ThreadIdentifier> id => _streamingService.Pull(id, checkpoint.Some(c => c > 0), cancellationToken),
            _ => _streamingService.PullAll(cancellationToken)
        };

        var mapResult = result.Map<StreamingEvent<ThreadIdentifier, Message, int>, object>(
           Request.Query.ContainsKey("plain") ? s => s.Event :
            s => _httpContextAccessor.GetMessageResource(s.Event, s.StreamId), cancellationToken);

        return Results.ServerSentEvents(mapResult);
    }
}