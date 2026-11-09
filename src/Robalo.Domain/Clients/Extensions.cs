using System.ClientModel;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using Robalo.Common.Configuration;
using Robalo.Common.Models;
using Robalo.Domain.Configuration;

namespace Robalo.Domain.Clients.Extensions;

public static class Extensions
{
    public static IServiceCollection AddSmartChatClient(
        this IServiceCollection services,
        IConfiguration configuration,
        string configurationKey = "Robalo:Llm")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);

        services.Configure<LlmOptions>(configuration.GetSection(key: configurationKey));

        services.AddSingleton<ISmart<ChatClient>>(sp =>
            new Smart<ChatClient, LlmOptions>(
                sp.GetRequiredService<IOptionsMonitor<LlmOptions>>(),
                options =>
                {
                    var settings = options.AsSettings();

                    return new ChatClient(
                        model: settings.Model,
                        credential: new ApiKeyCredential(settings.Key),
                        options: new OpenAIClientOptions { Endpoint = settings.Endpoint });
                }));

        return services;
    }
}