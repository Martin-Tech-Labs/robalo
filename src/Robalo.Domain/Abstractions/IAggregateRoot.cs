namespace Robalo.Domain.Abstractions;

public interface IAggregateRoot
{
    void When<TEvent>(TEvent @event);
}