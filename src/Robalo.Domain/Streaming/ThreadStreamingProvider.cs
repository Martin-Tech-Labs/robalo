using Hangfire.Logging;
using Robalo.Common.Models;
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

    public Task<IEnumerable<StreamingEvent<ThreadIdentifier, Message, int>>> GetEvents(ThreadIdentifier streamId, Option<int> fromExcluding, Option<int> toIncluding, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }

    public Task PushForStream(ThreadIdentifier streamId, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}