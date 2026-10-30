using System.Numerics;

namespace Robalo.Common.Streaming;

public sealed record StreamingEvent<TStreamId, TEvent, TCheckpoint>(
    TStreamId StreamId,
    TEvent Event,
    TCheckpoint Checkpoint)

    where TStreamId : notnull
    where TEvent : notnull
    where TCheckpoint : notnull, IComparisonOperators<TCheckpoint, TCheckpoint, bool>;