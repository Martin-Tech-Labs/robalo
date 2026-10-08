using Robalo.Common.Models;

namespace Robalo.Domain.Models;

public abstract class AggregateRoot<TSelf>(ThreadIdentifier id, Version version) where TSelf : AggregateRoot<TSelf>
{
    public ThreadIdentifier Id { get; } = id;
    public Option<Version> OldVersion { get; protected set; } = new None();
    public Version NewVersion { get; protected set; } = version;

    public IReadOnlyList<object> PendingEvents => _pendingEvents.AsReadOnly<object>();

    readonly Dictionary<Type, Action<object>> _eventHandlers = [];
    readonly List<object> _pendingEvents = [];

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
        }
        else
        {
            throw new InvalidOperationException($"Registration for event {@event.GetType().Name}");
        }

        return (TSelf)this;
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