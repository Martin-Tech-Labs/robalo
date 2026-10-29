using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Services;

public sealed class AnswerThreadJob
{
    readonly ILogger _logger;
    readonly IAnswerThreadService _service;

    public AnswerThreadJob(ILogger logger, IAnswerThreadService service)
    {
        _logger = logger.ForContext(GetType());
        _service = service;
    }

    public async Task Answer(string threadId, CancellationToken cancellationToken)
    {
        var result = await _service.Answer(ThreadIdentifier.FromIdString(threadId), cancellationToken);
        if(result is not Success)
        {
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new Exception($"{_service.GetType().Name} should have returned {nameof(Success)} but returned {result.Value.GetType().Name}");
#pragma warning restore CA2201 // Do not raise reserved exception types
        }
    }
}