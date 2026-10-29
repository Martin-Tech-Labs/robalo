using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Robalo.Common.Coordination.Extensions;

public static class Extensions
{
    public static IServiceCollection AddMessageBus(this IServiceCollection services, Func<IMessageBus, IMessageBus>? configure = null)
    {
        if (configure == null)
        {
            return services.AddSingleton<IMessageBus, MessageBus>();
        }

        return services.AddSingleton(serviceProvider =>
            configure(new MessageBus(serviceProvider.GetRequiredService<ILogger>(), serviceProvider)));
    }
}