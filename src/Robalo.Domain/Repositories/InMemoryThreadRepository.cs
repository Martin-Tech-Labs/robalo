using System.Runtime.CompilerServices;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;
using Serilog;

namespace Robalo.Domain.Repositories;

public sealed class InMemoryThreadRepository : IThreadRepository
{
    private sealed record ThreadStorage(
        string? Title,
        Version Version,
        DateTimeOffset CreatedOn,
        List<UserMessageAdded> Events);

    private readonly Dictionary<ThreadIdentifier, ThreadStorage> _threads = [];
    private readonly Lock _lock = new();
    private readonly ILogger _logger;

    public InMemoryThreadRepository(ILogger logger)
    {
        _logger = logger.ForContext(GetType());
    }

    public async Task<SuccessOrNotFound> DeleteThread(ThreadIdentifier identifier, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return _threads.Remove(identifier) ? new Success() : new NotFound();
        }
    }

    public async Task<Option<Thread>> GetThread(ThreadIdentifier identifier, CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            return GetThreadOrNone(identifier) switch
            {
                None none => none,
                Some<ThreadStorage> threadStorage =>
                    Thread.NewThread(identifier, threadStorage.Value.CreatedOn)
                    .ForEach(items: threadStorage.Value.Events, action: (t, i) => t.AddUserMessage(i.Content, i.AddedOn))
                    .DoIfNotNullOrWhiteSpace(threadStorage.Value.Title, (thr, title) => thr.UpdateTitle(title))
                    .MarkAggregateAsSynchronized(threadStorage.Value.Version)
            };
        }
    }

    public async Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken)
    {
        // No changes
        if (thread.OldVersion == thread.NewVersion)
        {
            return new NoChanges();
        }

        // Create
        if (!thread.OldVersion.HasValue)
        {
            lock (_lock)
            {
                if (_threads.ContainsKey(thread.Id))
                {
                    return new ConcurrencyConflict();
                }

                _threads[thread.Id] = new(
                          Title: thread.Title,
                          Version: thread.NewVersion,
                          CreatedOn: thread.CreatedOn,
                          Events: [.. thread.Messages.MapEach(UserMessageAdded.FromMessage)]);
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

            var threadStorage = threadOrNone.ValueOrFailure;

            _threads[thread.Id] = threadStorage with
            {
                Title = thread.Title,
                Version = thread.NewVersion,
            };

            threadStorage.Events.AddRange(thread.PendingEvents.Select(e => (UserMessageAdded)e));
        }

        thread.MarkAggregateAsSynchronized();
        return new Updated();
    }

    public async Task<Option<Message>> GetMessage(ThreadIdentifier identifier, int messageNumber, CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(messageNumber, 1);

        var threadOrNone = GetThreadOrNone(identifier);

        return threadOrNone switch
        {
            None none => none,
            Some<ThreadStorage> some => GetMessage(some.Value, messageNumber)
        };

        Option<Message> GetMessage(ThreadStorage thread, int messageNumber)
        {
            if (thread.Events.Count < messageNumber)
            {
                return None.Default;
            }

            UserMessageAdded userMessageAdded;

            lock (_lock)
            {
                userMessageAdded = thread.Events[messageNumber - 1];
            }
            return Message.FromUserMessageAdded(userMessageAdded, messageNumber);
        }
    }

    public async Task<Option<IAsyncEnumerable<Message>>> GetMessages(ThreadIdentifier identifier, CancellationToken cancellationToken)
    {
        var threadOrNone = GetThreadOrNone(identifier);

        return threadOrNone switch
        {
            None none => none,
            Some<ThreadStorage> some => GetEnumerable(some.Value.Events, cancellationToken).Some()
        };

        async IAsyncEnumerable<Message> GetEnumerable(List<UserMessageAdded> events, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var counter = 0;
            for (var i = 0; i < events.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UserMessageAdded item;
                lock (_lock)
                {
                    item = events[i];
                }

                yield return Message.FromUserMessageAdded(item, ++counter);
            }
        }
    }

    Option<ThreadStorage> GetThreadOrNone(ThreadIdentifier identifier)
    {
        lock (_lock)
        {
            if (_threads.TryGetValue(identifier, out var threadStorage))
            {
                return threadStorage;
            }
        }

        return new None();
    }

    public async Task<Option<QueryResult>> QueryMessages(ThreadIdentifier identifier, Cursor cursor, CancellationToken cancellationToken)
    {
        return GetThreadOrNone(identifier) switch
        {
            None none => none,
            Some<ThreadStorage> some => GetQueryResult(some.Value)
        };

        QueryResult GetQueryResult(ThreadStorage thread)
        {
            var eventsCount = thread.Events.Count;

            if (eventsCount == 0)
            {
                return QueryResult.Empty();
            }

            if (cursor.Reference.HasValue && cursor.Reference.ValueOrFailure > eventsCount)
            {
                return QueryResult.Empty();
            }

            var messages = GetMessages(thread, eventsCount).ToList();
            if (messages.Count == 0)
            {
                throw new InvalidOperationException("Unexpected empty list of messages");
            }

            Cursor self = Cursor.Create(messages.Count, messages[0].Number);
            Option<Cursor> prev = messages[0].Number == 1 ?
                None.Default :
                Cursor.Create(cursor.Limit, Math.Max(1, messages[0].Number - cursor.Limit));

            Option<Cursor> next = messages[^1].Number == eventsCount ?
                 None.Default :
                 Cursor.Create(cursor.Limit, messages[^1].Number + 1);

            return new(
                Messages: messages,
                Self: self,
                Prev: prev,
                Next: next);
        }

        IEnumerable<Message> GetMessages(ThreadStorage thread, int eventsCount)
        {
            var startIndex = cursor.Reference switch
            {
                Some<int> some => some - 1,
                _ => Math.Max(0, eventsCount - cursor.Limit )
            };

            var endIndex = Math.Min(eventsCount - 1, startIndex + cursor.Limit - 1);


            for (var i = startIndex; i <= endIndex; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                UserMessageAdded userMessageAdded;
                lock (_lock)
                {
                    userMessageAdded = thread.Events[i];
                }

                yield return Message.FromUserMessageAdded(userMessageAdded, i + 1);
            }
        }
    }
}