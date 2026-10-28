namespace Robalo.Common.Coordination;

public interface IMessageBus
{
    void Send<TMessage>(TMessage message) where TMessage : notnull;
    void Off<TMessage>(SubscriptionId<TMessage> subscriptionId) where TMessage : notnull;
    SubscriptionId<TMessage> On<TMessage>(Action<TMessage> action) where TMessage : notnull;
    SubscriptionId<TMessage> On<TMessage, THandler>(Action<TMessage, THandler> action)
        where TMessage : notnull
        where THandler : notnull;
}