using Hangfire.Logging;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Common.Streaming;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.Streaming;

public sealed class ThreadStreamingProvider : IStreamingProvider<ThreadIdentifier, Message, int>
{
    readonly ILogger _logger;
    readonly IThreadRepository _repository;

    public ThreadStreamingProvider(ILogger logger, IThreadRepository repository)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;
    }
    public async Task<IReadOnlyDictionary<ThreadIdentifier, int>> GetAllCheckpoints(CancellationToken cancellationToken)
        => (await _repository.ListThreads(cancellationToken)).ToDictionary(t => t.Id, t => t.Version);

    public async Task<IEnumerable<StreamingEvent<ThreadIdentifier, Message, int>>> GetEvents(
        ThreadIdentifier threadId,
        Option<int> fromExcludingOrNone,
        CancellationToken cancellationToken)
    {
        var from = fromExcludingOrNone switch { Some<int> some => Math.Max(1, some + 1), _ => 1 };

        Option<Cursor> next = Cursor.Create(10, from);

        var messages = new List<Message>();
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _repository.QueryMessages(threadId, next.ValueOrFailure, cancellationToken);
            if (result is Some<QueryResult> queryResult)
            {
                messages.AddRange(queryResult.Value.Messages);
                next = queryResult.Value.Next;
            }
            else
            {
                return [];
            }
        }
        while (next.HasValue);

        return messages.Select(m => new StreamingEvent<ThreadIdentifier, Message, int>(threadId, m, m.Number));
    }
}