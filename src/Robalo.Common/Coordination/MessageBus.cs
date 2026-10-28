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
        var lambda = (object message) => { action((TMessage)message); };
        subscriptionId = RegisterLambda<TMessage>(lambda);
        return this;
    }


    public IMessageBus On<TMessage, THandler>(Action<TMessage, THandler> action, out SubscriptionId<TMessage> subscriptionId)
        where TMessage : notnull
        where THandler : notnull
    {
        var lambda = (object message) =>
        {
            var handler = _serviceProvider.GetRequiredService<THandler>();
            action((TMessage)message, handler);
        };

        subscriptionId = RegisterLambda<TMessage>(lambda);
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

    private SubscriptionId<TMessage> RegisterLambda<TMessage>(Action<object> lambda) where TMessage : notnull
    {
        var id = Guid.NewGuid().ToString("N");
        var subscription = new Subscription(id, lambda);
        _subscribers.AddOrUpdate(
            typeof(TMessage),
            _ => [subscription],
            (_, list) => [.. list, subscription]);

        return new(id);
    }
}