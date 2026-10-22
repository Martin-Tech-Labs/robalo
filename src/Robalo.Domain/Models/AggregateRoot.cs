using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public abstract class AggregateRoot<TSelf> where TSelf : AggregateRoot<TSelf>
{
    public ThreadIdentifier Id { get; }
    public Option<int> PersistedVersion { get; protected set; } = None.Default;
    public int PendingVersion => _pendingEvents.Count + PersistedVersion.ValueOr(0);
    public IReadOnlyList<MessageAdded> PendingEvents { get; }

    readonly Dictionary<Type, Action<MessageAdded>> _eventHandlers = [];
    readonly List<MessageAdded> _pendingEvents = [];

    public AggregateRoot(ThreadIdentifier id, Option<int> persistedVersion)
    {
        Id = id;
        PersistedVersion = persistedVersion;
        PendingEvents = _pendingEvents.AsReadOnly();
    }

    protected void When<TEvent>(Action<TEvent> actionType) where TEvent : MessageAdded
    {
        if (_eventHandlers.ContainsKey(typeof(TEvent)))
        {
            throw new InvalidOperationException($"Duplicate registration for event {typeof(TEvent).Name}");
        }
        _eventHandlers[typeof(TEvent)] = obj => actionType((TEvent)obj);
    }



    protected TSelf Apply<TEvent>(TEvent @event) where TEvent : MessageAdded
    {
        if (_eventHandlers.TryGetValue(@event.GetType(), out var action))
        {
            action(@event);
            _pendingEvents.Add(@event);
            return (TSelf)this;
        }

        throw new InvalidOperationException($"Missing registration for event {@event.GetType().Name}");
    }

    protected TSelf ReplayEvents<TEvent>(IReadOnlyList<TEvent> events) where TEvent : MessageAdded
    {
        this.ForEach(events, (t, e) => Apply(e));
        _pendingEvents.Clear();
        return (TSelf)this;
    }

    public TSelf MarkAggregateAsSynchronized()
    {
        if (PersistedVersion == PendingVersion)
        {
            throw new InvalidOperationException("Cannot mark aggregate as synchronized: Aggregate is already synchonized");
        }

        PersistedVersion = PendingVersion;
        _pendingEvents.Clear();

        return (TSelf)this;
    }
}