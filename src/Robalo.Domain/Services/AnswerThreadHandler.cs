using Hangfire;
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
            s => s.Answer(threadUpdated.ThreadId, CancellationToken.None));
    }
}