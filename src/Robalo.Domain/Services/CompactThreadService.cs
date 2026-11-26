
using Robalo.Common.Extensions;
using Robalo.Common.Models;
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

        var model = "test";

        var index = thread.Context.ToList().FindLastIndex(c => c.PromptTokens.HasValue && c.Model == model);
        int contextSize = 0;
        if (index != -1 && thread.Context[index].Model == model && thread.Context[index].PromptTokens is Some<int> promptTokens)
        {
            contextSize = promptTokens;
        }
        else
        {
            index = 0;
        }

        for (var i = index; i < thread.Context.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var message = thread.Context[i];
            if (message.Model == model && message.Tokens is Some<int> tokens)
            {
                contextSize += tokens;
            }
            else
            {
                contextSize += (message.Content.Length + 3) / 4;
            }
        }
    }
}