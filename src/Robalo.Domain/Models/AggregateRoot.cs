namespace Robalo.Domain.Models;

public abstract class AggregateRoot
{
    public Identifier Id { get; }
    public Version OldVersion { get; protected set; }
    public Version NewVersion { get; protected set; }

    public IReadOnlyList<object> PendingEvents => _pendingEvents.AsReadOnly<object>();

    readonly Dictionary<Type, Action<object>> _eventHandlers = [];
    readonly List<object> _pendingEvents = [];

    protected AggregateRoot(Identifier id, Version version)
    {
        Id = id;
        OldVersion = NewVersion = version;
    }

    protected void When<TEvent>(Action<TEvent> actionType)
    {
        if (_eventHandlers.ContainsKey(typeof(TEvent)))
        {
            throw new InvalidOperationException($"Duplicate registration for event {typeof(TEvent).Name}");
        }
        _eventHandlers[typeof(TEvent)] = obj => actionType((TEvent)obj);
    }

    public void Apply<TEvent>(TEvent @event) where TEvent : notnull
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
    }

    public void MarkAggregateAsSynchronized()
    {
        _pendingEvents.Clear();
        OldVersion = NewVersion;
    }
}