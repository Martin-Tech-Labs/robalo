using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Robalo.Common.Coordination;

public sealed class MessageBus : IMessageBus
{
    record Subscription(string Id, Action<object> Action);
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

    public SubscriptionId<TMessage> On<TMessage>(Action<TMessage> action) where TMessage : notnull
    {
        var lambda = (object message) => { action((TMessage)message); };

        return RegisterLambda<TMessage>(lambda);
    }


    public SubscriptionId<TMessage> On<TMessage, THandler>(Action<TMessage, THandler> action)
        where TMessage : notnull
        where THandler : notnull
    {
        var lambda = (object message) =>
        {
            var handler = _serviceProvider.GetService<THandler>();
            if (handler is null)
            {
                throw new InvalidOperationException($"Failed to retrieve an instance of {typeof(THandler)}");
            }
            action((TMessage)message, handler);
        };

        return RegisterLambda<TMessage>(lambda);
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