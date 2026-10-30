using System.Collections.Concurrent;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Semaphore;
using Serilog;

namespace Robalo.Common.Streaming;

sealed record Subscription<TStreamId, TEvent, TCheckpoint>(
    ChannelWriter<StreamingEvent<TStreamId, TEvent, TCheckpoint>> ChannelWriter,
    Func<StreamingEvent<TStreamId, TEvent, TCheckpoint>, bool> Filter)
    where TStreamId : notnull
    where TEvent : notnull
    where TCheckpoint : notnull, IComparisonOperators<TCheckpoint, TCheckpoint, bool>;

public closed class StreamingService<TStreamId, TEvent, TCheckpoint> : IStreamingService<TStreamId, TEvent, TCheckpoint>, IDisposable
    where TEvent : notnull
    where TStreamId : notnull
    where TCheckpoint : notnull, IComparisonOperators<TCheckpoint, TCheckpoint, bool>
{
    readonly ConcurrentDictionary<Guid, Subscription<TStreamId, TEvent, TCheckpoint>> _channels = new();
    ConcurrentDictionary<TStreamId, TCheckpoint> _liveCheckpoints;
    readonly Dictionary<TStreamId, SemaphoreWrapper> _semaphores = new();
    private readonly SemaphoreWrapper _initSemaphore = new SemaphoreWrapper(new SemaphoreSlim(1, 1));

    readonly Lock _lockSemaphores = new();

    readonly IStreamingProvider<TStreamId, TEvent, TCheckpoint> _streamingProvider;

    readonly ILogger _logger;
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    public StreamingService(
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
        ILogger logger,
        IStreamingProvider<TStreamId, TEvent, TCheckpoint> streamingProvider)
    {
        _logger = logger.ForContext(GetType());
        _streamingProvider = streamingProvider;
    }

    async ValueTask InitializeCheckpoints(CancellationToken cancellationToken)
    {
        if (Volatile.Read(ref _liveCheckpoints) is not null)
        {
            return;
        }

        using var semaphore = _initSemaphore.Wait(cancellationToken);

        if (Volatile.Read(ref _liveCheckpoints) is not null)
        {
            return;
        }

        var checkpoints = await _streamingProvider.GetAllCheckpoints(cancellationToken);
        Volatile.Write(ref _liveCheckpoints, new ConcurrentDictionary<TStreamId, TCheckpoint>(checkpoints));
    }

    Task<IDisposable> AcquireSemaphore(TStreamId streamId, CancellationToken cancellationToken)
    {
        lock (_lockSemaphores)
        {
            if (_semaphores.TryGetValue(streamId, out var semaphoreSlim))
            {
                return semaphoreSlim.Wait(cancellationToken);
            }

            var semaphoreWrapper = new SemaphoreWrapper(new SemaphoreSlim(1, 1));
            _semaphores[streamId] = semaphoreWrapper;
            return semaphoreWrapper.Wait(cancellationToken);
        }
    }

    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
        TStreamId streamId,
        Option<TCheckpoint> checkpointOrNone,
        Func<TEvent, bool> filter,
        CancellationToken cancellationToken) => RegisterAndPull(
        filter: e => e.StreamId.Equals(streamId) && filter(e.Event),
        checkpointOrNone: checkpointOrNone switch
        {
            Some<TCheckpoint> some => (streamId, some),
            _ => None.Default
        },
        cancellationToken: cancellationToken);

    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
       Func<StreamingEvent<TStreamId, TEvent, TCheckpoint>, bool> filter,
       CancellationToken cancellationToken) => RegisterAndPull(filter, None.Default, cancellationToken);

    public IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> Pull(
        TStreamId streamId,
        Option<TCheckpoint> checkpointOrNone,
        CancellationToken cancellationToken) => RegisterAndPull(
            filter: e => e.StreamId.Equals(streamId),
            checkpointOrNone: checkpointOrNone switch
            {
                Some<TCheckpoint> some => (streamId, some),
                _ => None.Default
            },
            cancellationToken: cancellationToken);

    async IAsyncEnumerable<StreamingEvent<TStreamId, TEvent, TCheckpoint>> RegisterAndPull(
      Func<StreamingEvent<TStreamId, TEvent, TCheckpoint>, bool> filter,
      Option<(TStreamId, TCheckpoint)> checkpointOrNone,
      [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!cancellationToken.CanBeCanceled)
        {
            throw new ArgumentException("Must be cancellable", nameof(cancellationToken));
        }

        await InitializeCheckpoints(cancellationToken);

        var channel = Channel.CreateUnbounded<StreamingEvent<TStreamId, TEvent, TCheckpoint>>();
        var subscription = new Subscription<TStreamId, TEvent, TCheckpoint>(channel.Writer, filter);
        var id = Guid.NewGuid();

        if (checkpointOrNone is Some<(TStreamId, TCheckpoint)> checkpointSome)
        {
            var (streamId, checkpoint) = checkpointSome.Value;
            using (var semaphore = await AcquireSemaphore(streamId, cancellationToken))
            {
                var replayThrough = checkpoint;
                foreach (var e in (await _streamingProvider.GetEvents(
                    streamId: streamId,
                    fromExcluding: checkpoint,
                    toIncluding: None.Default,
                    cancellationToken)).Where(filter))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    await channel.Writer.WriteAsync(e, cancellationToken);
                    replayThrough = e.Checkpoint;
                }

                var originalFilter = subscription.Filter;

                subscription = subscription with
                {
                    Filter = e => originalFilter(e) && e.Checkpoint > replayThrough
                };

                _channels[id] = subscription;
            }
        }
        else
        {
            _channels[id] = subscription;
        }
        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken))
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return item;
            }
        }
        finally
        {
            _channels.TryRemove(id, out _);
            channel.Writer.TryComplete();
        }
    }

    public async Task PushForStream(
            TStreamId streamId,
            CancellationToken cancellationToken)
    {
        await InitializeCheckpoints(cancellationToken);
        using var semaphore = await AcquireSemaphore(streamId, cancellationToken);
        var events = await _streamingProvider.GetEvents(
           streamId: streamId,
           fromExcluding: _liveCheckpoints.TryGetValue(streamId, out var checkpoint) ? checkpoint : None.Default,
           toIncluding: None.Default,
           cancellationToken).ToList();

        if (events.Count == 0)
        {
            return;
        }

        foreach (var streamingEvent in events)
        {
            _liveCheckpoints[streamId] = streamingEvent.Checkpoint;
            var subscriptions = _channels.Values.Where(s => s.Filter(streamingEvent)).ToArray();
            foreach (var sub in subscriptions)
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    await sub.ChannelWriter.WriteAsync(streamingEvent, cancellationToken);
                }
                catch (ChannelClosedException)
                { }
            }
        }
    }

    public void Dispose()
    {
        foreach (var semaphore in _semaphores.Values)
        {
            semaphore.Dispose();
        }

        _initSemaphore.Dispose();

        GC.SuppressFinalize(this);
    }
}