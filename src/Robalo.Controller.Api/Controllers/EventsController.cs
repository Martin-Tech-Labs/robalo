using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
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
        [FromQuery(Name = "thread")] string? threadId,
        [FromQuery] int? checkpoint,
        CancellationToken cancellationToken)
    {

        return await GetThreadOrNone()
                  .Bind(threadOrNone => GetCheckpointOrNone(threadOrNone)
                  .Map(checkpointOrNone => Subscribe(threadOrNone, checkpointOrNone)))
                  .Either();

        IResult Subscribe(Option<Thread> threadOrNone, Option<int> checkpointOrNone)
        {
            var result = threadOrNone switch
            {
                Some<Thread> thread => _streamingService.Pull(thread.Value.Id, checkpointOrNone, cancellationToken),
                _ => _streamingService.PullAll(cancellationToken)
            };

            return Results.ServerSentEvents(result.Map(s => _httpContextAccessor.GetMessageResource(s.Event, s.StreamId), cancellationToken));
        }

        Result<Option<int>, IResult> GetCheckpointOrNone(Option<Thread> threadIdOrNone)
        {
            var isValidCheckpoint = threadIdOrNone switch
            {
                Some<Thread> thread => checkpoint is null || (checkpoint >= 1 && checkpoint <= thread.Value.PersistedVersion.ValueOrFailure),
                _ => checkpoint is null
            };

            return isValidCheckpoint ? checkpoint.AsOption() : Results.NotFound();
        }

        async Task<Result<Option<Thread>, IResult>> GetThreadOrNone()
        {
            if (string.IsNullOrWhiteSpace(threadId))
            {
                return Option<Thread>.None;
            }

            var threadIdOrNone = ThreadIdentifier.TryGetFromId(threadId);
            if (threadIdOrNone is not Some<ThreadIdentifier> threadIdentifier)
            {
                return Results.NotFound();
            }

            var threadOrNone = await _repository.GetThread(threadIdentifier, cancellationToken);

            return threadOrNone.HasValue ?
             threadOrNone :
             Results.NotFound();
        }
    }
}