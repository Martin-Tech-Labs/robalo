using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
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
    public async Task<IActionResult> CreateThread(CreateThreadApiRequest request, CancellationToken cancellationToken)
    {
        var utcNow = _dateTimeOffsetProvider.UtcKnow;

        var thread = Thread.NewThread(Identifier.NewThreadId(), utcNow)
         .AddUserMessage(Identifier.NewMessageId(), request.Content!, utcNow);

        var result = await _threadRepository.SaveThread(thread, cancellationToken);

        return result switch
        {
            Created _ => GetCreatedResult(),
            ConcurrencyConflict => GetConcurrencyResult(),
            Updated => throw new InvalidOperationException("Unexpected result Updated on thread creation"),
            NoChanges => throw new InvalidOperationException("Unexpected result NoChanges on thread creation")
        };

        IActionResult GetConcurrencyResult()
        {
            var problem = _problemDetailsFactory.CreateProblemDetails(
            httpContext: HttpContext,
            statusCode: StatusCodes.Status409Conflict,
            title: "Conflict",
            detail: "thread_already_exists");

            return new ConflictObjectResult(problem);
        }

        IActionResult GetCreatedResult()
        {
            var resource = new ThreadResource(
                thread.CreatedOn,
                thread.ModifiedOn,
                thread.Title,
                thread.Id.Id,
                new ThreadLinks(
                    Self: HttpContext.LinkToPath($"/threads/{thread.Id}"),
                    Messages: HttpContext.LinkToPath($"/threads/{thread.Id}/messages"),
                    Events: HttpContext.LinkToPath($"/events?thread_id={thread.Id}")));


            return new CreatedResult(resource._Links.Self.Href.AbsoluteUri, resource);
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        _logger.Information("Getting..");
        return Ok("Hi there");
    }
}