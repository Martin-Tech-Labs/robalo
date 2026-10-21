using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public abstract class AggregateRoot<TSelf> where TSelf : AggregateRoot<TSelf>
{
    public ThreadIdentifier Id { get; }
    public Option<Version> OldVersion { get; protected set; } = None.Default;
    public Version NewVersion { get; protected set; }

    public IReadOnlyList<MessageAdded> PendingEvents { get; }

    readonly Dictionary<Type, Action<MessageAdded>> _eventHandlers = [];
    readonly List<MessageAdded> _pendingEvents = [];

    public AggregateRoot(ThreadIdentifier id, Version version)
    {
        Id = id;
        NewVersion = version;
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

    public TSelf MarkAggregateAsSynchronized()
    {
        if (_pendingEvents.Count == 0 && OldVersion == NewVersion)
        {
            throw new InvalidOperationException("Cannot mark aggregate as synchronized: Aggregate is already synchonized");
        }

        _pendingEvents.Clear();
        OldVersion = NewVersion;

        return (TSelf)this;
    }
}