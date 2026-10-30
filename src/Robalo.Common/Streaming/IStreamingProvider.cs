using System.Numerics;
using Robalo.Common.Models;
using Robalo.Common.Services;

namespace Robalo.Common.Streaming;

public interface IStreamingProvider<TStreamId, TEvent, TCheckpoint>
    where TStreamId : notnull
    where TEvent : notnull
    where TCheckpoint : notnull, IComparisonOperators<TCheckpoint, TCheckpoint, bool>
{
    public Task<IEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>>> GetEvents(
        TStreamId streamId,
        Option<TCheckpoint> fromExcluding,
        Option<TCheckpoint> toIncluding,
        CancellationToken cancellationToken);

    public Task<IReadOnlyDictionary<TStreamId, TCheckpoint>> GetAllCheckpoints(CancellationToken cancellationToken);

    public Task PushForStream(TStreamId streamId, CancellationToken cancellationToken);
}