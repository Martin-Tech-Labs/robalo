using System.Numerics;
using System.Threading.Channels;
using Robalo.Common.Models;

namespace Robalo.Common.Streaming;

public interface IStreamingService<TStreamId, TEvent, TCheckpoint>
    where TStreamId : notnull
    where TEvent : notnull
    where TCheckpoint : notnull, IComparisonOperators<TCheckpoint, TCheckpoint, bool>
{
    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
        TStreamId streamId,
        Option<TCheckpoint> checkpointOrNone,
        CancellationToken cancellationToken);

    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
        Func<StreamingEvent<TStreamId, TEvent, TCheckpoint>, bool> filter,
        CancellationToken cancellationToken);

    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
        TStreamId streamId,
        Option<TCheckpoint> checkpointOrNone,
        Func<TEvent, bool> filter,
        CancellationToken cancellationToken);

    public Task PushForStream(TStreamId streamId, CancellationToken cancellationToken);
}