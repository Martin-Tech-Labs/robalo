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
        Option<int> fromExcluding,
        Option<int> toIncluding,
        CancellationToken cancellationToken)
    {
        var limit = 10;
        if (fromExcluding is Some<int> from && toIncluding is Some<int> to)
        {
            limit = to - from;
        }

        Option<Cursor> next = Cursor.Create(10, fromExcluding switch { Some<int> some => some + 1, _ => 1 });

        var messages = new List<Message>();
        QueryResult queryResult;
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _repository.QueryMessages(threadId, next.ValueOrFailure, cancellationToken);
            if (!result.HasValue)
            {
                return Enumerable.Empty<StreamingEvent<ThreadIdentifier, Message, int>>();
            }
            queryResult = result.ValueOrFailure;
            messages.AddRange(queryResult.Messages);
            next = queryResult.Next;
        }
        while (next.HasValue && !toIncluding.HasValue);
        
        return messages.Select(m => new StreamingEvent<ThreadIdentifier, Message, int>(threadId, m, m.Number));
    }
}