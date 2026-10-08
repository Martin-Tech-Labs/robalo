using Robalo.Common.Models;

namespace Robalo.Domain.Models;

public abstract class AggregateRoot<TSelf> where TSelf : AggregateRoot<TSelf>
{
    public ThreadIdentifier Id { get; }
    public Option<Version> OldVersion { get; protected set; } = new None();
    public Version NewVersion { get; protected set; }

    public IReadOnlyList<object> PendingEvents { get; }

    readonly Dictionary<Type, Action<object>> _eventHandlers = [];
    readonly List<object> _pendingEvents = [];

    public AggregateRoot(ThreadIdentifier id, Version version)
    {
        Id = id; NewVersion = version;
        PendingEvents = _pendingEvents.AsReadOnly<object>();
    }

    protected void When<TEvent>(Action<TEvent> actionType)
    {
        if (_eventHandlers.ContainsKey(typeof(TEvent)))
        {
            throw new InvalidOperationException($"Duplicate registration for event {typeof(TEvent).Name}");
        }
        _eventHandlers[typeof(TEvent)] = obj => actionType((TEvent)obj);
    }

    public TSelf Apply<TEvent>(TEvent @event) where TEvent : notnull
    {
        if (_eventHandlers.TryGetValue(@event.GetType(), out var action))
        {
            action(@event);
            _pendingEvents.Add(@event);
            return (TSelf)this;
        }

        throw new InvalidOperationException($"Missing registration for event {@event.GetType().Name}");
    }

    public TSelf MarkAggregateAsSynchronized(Option<Version> version = default)
    {
        _pendingEvents.Clear();

        _ = version switch
        {
            Some<Version> some => OldVersion = NewVersion = some,
            _ => OldVersion = NewVersion
        };

        return (TSelf)this;
    }
}