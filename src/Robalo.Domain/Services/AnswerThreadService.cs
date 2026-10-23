using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Domain.Clients;
using Robalo.Domain.Clients.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.Services;

public class AnswerThreadService : IAnswerThreadService
{
    readonly ILogger _logger;
    readonly ILlmClient _llmClient;
    readonly IThreadRepository _threadRepository;
    readonly IDateTimeOffsetProvider _dateTimeOffSetProvider;

    public AnswerThreadService(
        ILogger logger,
        ILlmClient llmClient,
        IThreadRepository threadRepository,
        IDateTimeOffsetProvider dateTimeOffsetProvider)
    {
        _logger = logger.ForContext(GetType());
        _llmClient = llmClient;
        _threadRepository = threadRepository;
        _dateTimeOffSetProvider = dateTimeOffsetProvider;
    }

    public async Task Answer(ThreadIdentifier threadIdentifier, CancellationToken cancellationToken)
    {
        var threadOrNone = await _threadRepository.GetThread(threadIdentifier, cancellationToken);

        if (!threadOrNone.HasValue)
        {
            _logger.Warning("Thread {ThreadId} does not exist", threadIdentifier);
            return;
        }

        var thread = threadOrNone.ValueOrFailure;

        if (thread.IsAnswered)
        {
            _logger.Information("Thread {ThreadId} is already answered", threadIdentifier);
            return;
        }

        if (thread.Messages.Count == 0)
        {
            _logger.Warning("Thread {ThreadId} contains no messages", threadIdentifier);
            return;
        }

        var llmRequest = new LlmRequest(string.Concat(thread.Messages));

        var lastMessage = thread.Messages[^1].Number;

        var llmResponse = await _llmClient.GetResponse(llmRequest, cancellationToken);

        if (llmResponse is LlmResponse response)
        {
            thread.AddLlmMessage(response.Content, lastMessage, _dateTimeOffSetProvider.UtcNow);
            var result = await _threadRepository.SaveThread(thread, cancellationToken);


        }


    }
}