
using Robalo.Domain.Clients;

namespace Robalo.Domain.Services;

public sealed class CompactThreadService : ICompactThreadService
{
    readonly ILogger _logger;
    readonly ILlmClient _llmClient;

    public CompactThreadService(ILogger logger, ILlmClient llmClient)
    {
        _logger = logger.ForContext(GetType());
        _llmClient = llmClient;
    }

    public async Task CompactAndRefreshThread(Thread thread, CancellationToken cancellationToken)
    {
        if (!thread.PersistedVersion.HasValue || thread.PersistedVersion != thread.PendingVersion)
        {
            throw new InvalidOperationException("Thread must not have pending events");
        }



    }
}