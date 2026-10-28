namespace Robalo.Common.Coordination;

public interface IMessageBus
{
    void Send<TMessage>(TMessage message) where TMessage : notnull;
    void Off<TMessage>(SubscriptionId<TMessage> subscriptionId) where TMessage : notnull;
    IMessageBus On<TMessage>(Action<TMessage> action, out SubscriptionId<TMessage> subscriptionId) where TMessage : notnull;
    IMessageBus On<TMessage, THandler>(Action<TMessage, THandler> action, out SubscriptionId<TMessage> subscriptionId)
        where TMessage : notnull
        where THandler : notnull;
}