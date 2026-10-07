using System.Runtime.CompilerServices;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Serilog;

namespace Robalo.Domain.Repositories;

public sealed class InMemoryThreadRepository : IThreadRepository
{
    private record ThreadStorage(
        string? Title,
        Version Version,
        DateTimeOffset CreatedOn,
        List<UserMessageAdded> Events);

    private readonly Dictionary<Identifier, ThreadStorage> _threads = [];
    private readonly Lock _lock = new();
    private readonly ILogger _logger;

    public InMemoryThreadRepository(ILogger logger)
    {
        _logger = logger.ForContext(GetType());
    }

    public async Task<SuccessOrNotFound> DeleteThread(Identifier identifier, CancellationToken cancellationToken)
    {
        if (identifier.Type != Identifier.IdentifierType.Thread)
        {
            throw new ArgumentException("Wrong identifier type", nameof(identifier));
        }

        lock (_lock)
        {
            return _threads.Remove(identifier) ? new Success() : new NotFound();
        }
    }

    public async Task<Option<Thread>> GetThread(Identifier identifier, CancellationToken cancellationToken)
    {
        if (identifier.Type != Identifier.IdentifierType.Thread)
        {
            throw new ArgumentException("Wrong identifier type", nameof(identifier));
        }

        return GetThreadOrNone(identifier) switch
        {
            None none => none,
            Some<ThreadStorage> threadStorage =>
                Thread.NewThread(identifier, threadStorage.Value.CreatedOn)
                .ForEach(items: threadStorage.Value.Events, action: (t, i) => t.AddUserMessage(i.Id, i.Content, i.AddedOn))
                .DoIfNotNullOrWhiteSpace(threadStorage.Value.Title, (thr, title) => thr.UpdateTitle(title))
                .MarkAggregateAsSynchronized(threadStorage.Value.Version)
        };
    }

    public async Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken)
    {
        if (thread.OldVersion == thread.NewVersion)
        {
            return new NoChanges();
        }

        var threadStorage = new ThreadStorage(
            Title: thread.Title,
            Version: thread.NewVersion,
            CreatedOn: thread.CreatedOn,
            Events: [.. thread.Messages.MapEach(UserMessageAdded.FromMessage)]);

        // Create
        if (!thread.OldVersion.HasValue)
        {
            lock (_lock)
            {
                if (_threads.ContainsKey(thread.Id))
                {
                    return new ConcurrencyConflict();
                }

                _threads[thread.Id] = threadStorage;
            }

            thread.MarkAggregateAsSynchronized();
            return new Created();
        }

        // Update
        lock (_lock)
        {
            var threadOrNone = GetThreadOrNone(thread.Id);
            if (!threadOrNone.HasValue)
            {
                return new ConcurrencyConflict();
            }

            if (threadOrNone.ValueOrFailure.Version != thread.OldVersion)
            {
                return new ConcurrencyConflict();
            }

            _threads[thread.Id] = threadStorage;
        }

        thread.MarkAggregateAsSynchronized();
        return new Updated();
    }

    public async Task<Option<IAsyncEnumerable<Message>>> GetMessages(Thread thread, CancellationToken cancellationToken)
    {
        var threadOrNone = GetThreadOrNone(thread.Id);

        return threadOrNone switch
        {
            None none => none,
            Some<ThreadStorage> some => GetEnumerable(some.Value.Events, cancellationToken).Some()
        };

        static async IAsyncEnumerable<Message> GetEnumerable(IEnumerable<UserMessageAdded> events, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (var item in events)
            {
                cancellationToken.ThrowIfCancellationRequested();
                yield return Message.FromUserMessageAdded(item);
            }
        }
    }

    Option<ThreadStorage> GetThreadOrNone(Identifier identifier)
    {
        lock (_lock)
        {
            if (_threads.TryGetValue(identifier, out var threadStorage))
            {
                return threadStorage with { Events = [.. threadStorage.Events] };
            }
        }

        return new None();
    }
}