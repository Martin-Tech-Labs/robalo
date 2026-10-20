using System.ClientModel;
using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenAI;
using OpenAI.Chat;
using Robalo.Common.Models;
using Robalo.Domain.Services.Configuration;

namespace Robalo.Domain.Services.Extensions;

public static class Extensions
{
    public static IServiceCollection AddLlmService(
        this IServiceCollection services,
        ConfigurationManager configuration,
        string configurationKey = "Llm")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);

        services.AddTransient<ILlmService, LlmService>();

        LlmOptions llmOptions = new();

        configuration.GetSection(configurationKey).Bind(llmOptions);

        var llmSettings = llmOptions.AsSettings();
        services.AddSingleton(llmSettings);

        var options = new OpenAIClientOptions();
        if (llmSettings.Endpoint is Some<Uri> uri)
        {
            options.Endpoint = uri;
        }
        var client = new ChatClient(
            model: llmSettings.Model,
            credential: new ApiKeyCredential(llmSettings.Key),
            options: options);

        services.AddSingleton(client);
        return services;
    }
}