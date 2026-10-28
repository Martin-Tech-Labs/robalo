using Hangfire;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.MessageBus;

namespace Robalo.Domain.Services;

public sealed class AnswerThreadHandler : IAnswerThreadHandler
{
    readonly IAnswerThreadService _answerThreadService;
    readonly ILogger _logger;

    public AnswerThreadHandler(ILogger logger, IAnswerThreadService answerThreadService)
    {
        _logger = logger.ForContext(GetType());
        _answerThreadService = answerThreadService;
    }

    public void Handle(ThreadUpdated threadUpdated)
    {
        BackgroundJob.Enqueue<IAnswerThreadService>(
            s => Answer(s, threadUpdated.ThreadId, CancellationToken.None));


    }

    async Task Answer(IAnswerThreadService service, ThreadIdentifier threadId, CancellationToken cancellationToken)
    {
        var result = await service.Answer(threadId, cancellationToken);
        if (result is not Success)
        {
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new Exception($"{nameof(IAnswerThreadService)} returned {result.Value} but expected {nameof(Success)}");
#pragma warning restore CA2201 // Do not raise reserved exception types
        }
    }
}