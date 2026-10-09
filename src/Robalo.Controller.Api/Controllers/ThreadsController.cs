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
[Route("threads")]
public class ThreadsController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IThreadRepository _threadRepository;
    private readonly IDateTimeOffsetProvider _dateTimeOffsetProvider;
    private readonly ProblemDetailsFactory _problemDetailsFactory;

    private const int _queryLimit = 10;

    public ThreadsController(
        ILogger logger,
        IThreadRepository threadRepository,
        ProblemDetailsFactory problemDetailsFactory,
        IDateTimeOffsetProvider dateTimeOffsetProvider)
    {
        _logger = logger.ForContext(GetType());
        _threadRepository = threadRepository;
        _dateTimeOffsetProvider = dateTimeOffsetProvider;
        _problemDetailsFactory = problemDetailsFactory;
    }

    [HttpPost("")]
    public async Task<IActionResult> CreateThread(MessageApiRequest request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeOffsetProvider.UtcNow;

        var thread = Thread.NewThread(ThreadIdentifier.NewIdentifier(), utcNow)
         .AddUserMessage(request.Content!, utcNow);

        var result = await _threadRepository.SaveThread(thread, cancellationToken);

        return result switch
        {
            Created _ => GetThreadResource(thread).AsCreated(),
            ConcurrencyConflict => GetConcurrencyResult("thread_already_exists"),
            Updated => throw new InvalidOperationException("Unexpected result Updated on thread creation"),
            NoChanges => throw new InvalidOperationException("Unexpected result NoChanges on thread creation")
        };
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetThread(string id, CancellationToken cancellationToken)
    {
        var threadIdOrNone = ThreadIdentifier.TryGetFromId(id);
        if (!threadIdOrNone.HasValue)
        {
            return NotFound();
        }

        var threadOrNone = await _threadRepository.GetThread(threadIdOrNone.ValueOrFailure, cancellationToken);
        return threadOrNone switch
        {
            Some<Thread> some => Ok(GetThreadResource(some.Value)),
            _ => NotFound()
        };
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteThread(string id, CancellationToken cancellationToken)
    {
        var threadIdOrNone = ThreadIdentifier.TryGetFromId(id);
        if (!threadIdOrNone.HasValue)
        {
            return NotFound();
        }

        return await _threadRepository.DeleteThread(threadIdOrNone.ValueOrFailure, cancellationToken) switch
        {
            Success => NoContent(),
            NotFound _ => NotFound()
        };
    }

    // [HttpGet("{id}/messages")]
    // public async Task<IActionResult> GetMessages(
    //     string id,
    //     CancellationToken cancellationToken)
    // {
    //     var threadIdOrNone = ThreadIdentifier.TryGetFromId(id);
    //     if (!threadIdOrNone.HasValue)
    //     {
    //         return NotFound();
    //     }

    //     return await _threadRepository.QueryMessages(threadIdOrNone.ValueOrFailure, cancellationToken) switch
    //     {
    //         None => NotFound(),
    //         Some<IAsyncEnumerable<Message>> messages => NotFound()
    //     };

    //     void test(IAsyncEnumerable<Message> messages)
    //     {
    //         // messages.
    //     }
    // }

    [HttpGet("{id}/messages/{number}")]
    public async Task<IActionResult> GetMessage(string id, int number, CancellationToken cancellationToken)
    {
        var threadIdOrNone = ThreadIdentifier.TryGetFromId(id);
        if (!threadIdOrNone.HasValue)
        {
            return NotFound();
        }

        if (number < 1)
        {
            return NotFound();
        }

        return await _threadRepository.GetMessage(threadIdOrNone.ValueOrFailure, number, cancellationToken) switch
        {
            None => NotFound(),
            Some<Message> some => GetMessageResource(some.Value, threadIdOrNone.ValueOrFailure).AsOk()
        };
    }

    [HttpPost("{id}/messages")]
    public async Task<IActionResult> CreateMessage(string id, MessageApiRequest request, CancellationToken cancellationToken)
    {
        var threadIdOrNone = ThreadIdentifier.TryGetFromId(id);
        if (!threadIdOrNone.HasValue)
        {
            return NotFound();
        }

        var threadId = threadIdOrNone.ValueOrFailure;

        var threadOrNone = await _threadRepository.GetThread(threadId, cancellationToken);

        if (threadOrNone.IsNone)
        {
            return NotFound();
        }

        var thread = threadOrNone.ValueOrFailure;
        thread.AddUserMessage(request.Content!, _dateTimeOffsetProvider.UtcNow);

        var result = await _threadRepository.SaveThread(thread, cancellationToken);

        return result switch
        {
            Created _ => throw new InvalidOperationException("Unexpected result Created on thread update"),
            ConcurrencyConflict => GetConcurrencyResult("thread_updated_in_parallel"),
            Updated => GetMessageResource(thread.Messages[thread.Messages.Count - 1], threadId).AsCreated(),
            NoChanges => throw new InvalidOperationException("Unexpected result NoChanges on thread update")
        };
    }

    ThreadResource GetThreadResource(Thread thread) => new(
                  thread.CreatedOn,
                  thread.ModifiedOn,
                  thread.Title,
                  thread.Id.Id,
                  new ThreadLinks(
                      Self: HttpContext.LinkToPath($"/threads/{thread.Id}"),
                      Messages: HttpContext.LinkToPath($"/threads/{thread.Id}/messages"),
                      Events: HttpContext.LinkToPath($"/events?thread_id={thread.Id}")));

    MessageResource GetMessageResource(Message message, ThreadIdentifier threadId) => new(
        message.Content,
        message.CreatedOn,
        message.Source.ToString(),
        threadId.Id,
        message.Number,
        new MessageLinks(
            Self: HttpContext.LinkToPath($"/threads/{threadId}/messages/{message.Number}"),
            Thread: HttpContext.LinkToPath($"/threads/{threadId}"),
            Events: HttpContext.LinkToPath($"/events?thread_id={threadId}")));

    ConflictObjectResult GetConcurrencyResult(string detail) => new(
        _problemDetailsFactory.CreateProblemDetails(
            httpContext: HttpContext,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            detail: detail));
}