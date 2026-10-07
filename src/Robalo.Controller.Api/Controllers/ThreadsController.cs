using Microsoft.AspNetCore.Mvc;
using Robalo.Controller.Api.Requests;
using Robalo.Domain.Repositories;

namespace Robalo.Controller.Api.Controllers;

[ApiController]
[Route("threads")]
public class ThreadsController : ControllerBase
{
    private readonly ILogger _logger;
    private readonly IThreadRepository _threadRepository;
    public ThreadsController(ILogger logger, IThreadRepository threadRepository)
    {
        _logger = logger.ForContext(GetType());
        _threadRepository = threadRepository;
    }

    [HttpPost("")]
    public async Task<IActionResult> CreateThread(CreateThreadApiRequest request)
    {
        _logger.Information("Getting..");
        return Ok("Hi there");
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(string id)
    {
        _logger.Information("Getting..");
        return Ok("Hi there");
    }
}