namespace Robalo.Domain;

public class AggregateRoot
{
    readonly Dictionary<Type, Action<object>> _eventHandlers = [];

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
        if (_eventHandlers.TryGetValue(typeof(TEvent), out var action))
        {
            action(@event);
        }
        else
        {
            throw new InvalidOperationException($"Registration for event {typeof(TEvent).Name}");
        }
    }
}