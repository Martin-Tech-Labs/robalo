using Hangfire;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.MessageBus;

namespace Robalo.Domain.Services;

public sealed class AnswerThreadHandler : IAnswerThreadHandler
{
    readonly ILogger _logger;

    public AnswerThreadHandler(ILogger logger)
    {
        _logger = logger.ForContext(GetType());
    }

    public void Handle(ThreadUpdated threadUpdated)
    {
        BackgroundJob.Enqueue<AnswerThreadJob>(
            s => s.Answer(threadUpdated.ThreadId.IdString, CancellationToken.None));
    }
}