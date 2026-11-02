using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Common.Streaming;
using Robalo.Domain.Models;
using Robalo.Domain.Repositories;
using Serilog;
using System.Runtime.CompilerServices;

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

    public async IAsyncEnumerable<StreamingEvent<ThreadIdentifier, Message, int>> GetEvents(
        ThreadIdentifier threadId,
        Option<int> fromExcludingOrNone,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var from = fromExcludingOrNone switch { Some<int> some => Math.Max(1, some + 1), _ => 1 };

        Option<Cursor> next = Cursor.Create(10, from);
        
        do
        {
            cancellationToken.ThrowIfCancellationRequested();
            var result = await _repository.QueryMessages(threadId, next.ValueOrFailure, cancellationToken);
            if (result is Some<QueryResult> queryResult)
            {
                foreach (var message in queryResult.Value.Messages)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    yield return new StreamingEvent<ThreadIdentifier, Message, int>(threadId, message, message.Number);
                }

                next = queryResult.Value.Next;
            }
            else
            {
                yield break;
            }
        }
        while (next.HasValue);
    }
}