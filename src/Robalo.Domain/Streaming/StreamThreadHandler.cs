using Hangfire;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.MessageBus;
using Robalo.Domain.Streaming;

namespace Robalo.Domain.Services;

public sealed class StreamThreadHandler : IStreamThreadHandler
{
    readonly ILogger _logger;

    public StreamThreadHandler(ILogger logger)
    {
        _logger = logger.ForContext(GetType());
    }

    public void Handle(ThreadUpdated threadUpdated)
    {
        BackgroundJob.Enqueue<StreamThreadJob>(
            s => s.Stream(threadUpdated.ThreadId.IdString, CancellationToken.None));
    }
}