using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Clients;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;

namespace Robalo.Domain.Services;

public sealed class ThreadService : IThreadService
{
    readonly ILogger _logger;
    readonly IThreadRepository _threadRepository;
    readonly ILlmClient _llmClient;

    public ThreadService(ILogger logger, IThreadRepository threadRepository, ILlmClient llmClient)
    {
        _logger = logger.ForContext(GetType());
        _llmClient = llmClient;
        _threadRepository = threadRepository;
    }

    public async Task ProcessUnansweredMessages(ThreadIdentifier threadIdentifier, CancellationToken cancellationToken)
    {
        var threadOrNone = await _threadRepository.GetThread(threadIdentifier, cancellationToken);

        if (!threadOrNone.HasValue)
        {
            return;
        }

        var thread = threadOrNone.ValueOrFailure;

        

        throw new NotImplementedException();
    }
}