using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Robalo.Common.Coordination;

public sealed class MessageBus : IMessageBus
{
    sealed record Subscription(string Id, Action<object> Action);
    readonly ILogger _logger;
    readonly ConcurrentDictionary<Type, IReadOnlyList<Subscription>> _subscribers = new();
    readonly IServiceProvider _serviceProvider;
    public MessageBus(ILogger logger, IServiceProvider serviceProvider)
    {
        _logger = logger.ForContext(GetType());
        _serviceProvider = serviceProvider;
    }

    public void Off<TMessage>(SubscriptionId<TMessage> subscriptionId) where TMessage : notnull
    {
        _subscribers.AddOrUpdate(
            typeof(TMessage),
            _ => [],
            (_, list) => [.. list.Where(l => l.Id != subscriptionId.Id)]);
    }

    public IMessageBus On<TMessage>(Action<TMessage> action, out SubscriptionId<TMessage> subscriptionId) where TMessage : notnull
    {
        var id = new SubscriptionId<TMessage>(Guid.NewGuid().ToString("N"));

        var lambda = (object message) =>
        {
            try
            {
                action((TMessage)message);
            }
            catch (Exception exc)
            {
                _logger.Error(exc, "Error during execution of lambda for type {TMessage} for Subscription {SubscriptionId}",
                     typeof(TMessage), id.Id);
            }
        };
        RegisterLambda(lambda, id);
        subscriptionId = id;
        return this;
    }


    public IMessageBus On<TMessage, THandler>(Action<TMessage, THandler> action, out SubscriptionId<TMessage> subscriptionId)
        where TMessage : notnull
        where THandler : notnull
    {
        var id = new SubscriptionId<TMessage>(Guid.NewGuid().ToString("N"));
        var lambda = (object message) =>
        {
            try
            {
                var handler = _serviceProvider.GetService<THandler>();
                if (handler is null)
                {
                    _logger.Warning("Could not get service {THandler} from Service Provider for Subscription {SubscriptionId} of type {TMessage}. This subscriber will be skipped.",
                        typeof(THandler), id.Id, typeof(TMessage));
                    return;
                }
                action((TMessage)message, handler);
            }

            catch (Exception exc)
            {
                _logger.Error(exc, "Error during execution of handler {THandler} of type {TMessage} for Subscription {SubscriptionId}",
                    typeof(THandler), typeof(TMessage), id.Id);
            }
        };

        RegisterLambda(lambda, id);
        subscriptionId = id;
        return this;
    }

    public void Send<TMessage>(TMessage message) where TMessage : notnull
    {
        if (_subscribers.TryGetValue(typeof(TMessage), out var subscribers))
        {
            foreach (var subscriber in subscribers)
            {
                subscriber.Action(message);
            }
        }
    }

    private void RegisterLambda<TMessage>(Action<object> lambda, SubscriptionId<TMessage> subscriptionId) where TMessage : notnull
    {
        var subscription = new Subscription(subscriptionId.Id, lambda);
        _subscribers.AddOrUpdate(
            typeof(TMessage),
            _ => [subscription],
            (_, list) => [.. list, subscription]);
    }
}