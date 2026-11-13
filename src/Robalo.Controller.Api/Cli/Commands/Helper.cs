using System.ClientModel;
using System.Globalization;
using OpenAI;
using OpenAI.Models;
using Robalo.Common.Models;
using Robalo.Domain.Configuration;
using Spectre.Console;

namespace Robalo.Controller.Api.Cli.Commands;

public static class Helper
{
    public static async Task<Option<LlmOptions>> GetLlmOptionsInteractive(CancellationToken cancellationToken, LlmOptions? llmOptions)
    {
        var endpointChoices = new Dictionary<string, Uri>
        {
            ["Ollama locally (http://localhost:11500/v1/)"] =
            new("http://localhost:11500/v1/"),
            ["OpenAI (https://api.openai.com/v1/)"] =
            new("https://api.openai.com/v1/"),
            ["Anthropic (https://api.anthropic.com/v1)"] =
            new("https://api.anthropic.com/v1")
        };

        if (llmOptions != null)
        {
            endpointChoices.Add($"Currently configured endpoint ({llmOptions.Endpoint})", llmOptions.Endpoint!);
        }

        const string custom = "Custom";

        var choice = await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title("Choose an LLM endpoint")
                .AddChoices(endpointChoices.Keys.Append(custom)), cancellationToken);

        var endpoint = choice == custom
            ? await AnsiConsole.PromptAsync(
                new TextPrompt<Uri>("Enter a custom OpenAI-compatible LLM endpoint")
                    .Validate(
                        uri => uri.IsAbsoluteUri &&
                            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                        "[red]Enter an absolute HTTP(S) URI.[/]"),
                cancellationToken)
            : endpointChoices[choice];

        string key;

        if (llmOptions is null || endpoint != llmOptions.Endpoint)
        {
            key = await AnsiConsole.PromptAsync(new TextPrompt<string>("Enter an API Key").Secret(), cancellationToken);
        }
        else
        {
            key = await AnsiConsole.PromptAsync(new TextPrompt<string>("Enter an API Key or press <Enter> to re-use the one currently configured").AllowEmpty().Secret(), cancellationToken);
            if (string.IsNullOrWhiteSpace(key))
            {
                key = llmOptions.Key!;
            }
        }

        var client = new OpenAIClient(new ApiKeyCredential(key), new OpenAIClientOptions { Endpoint = endpoint });

        ClientResult<OpenAIModelCollection> models;
        try
        {
            models = await client
                .GetOpenAIModelClient()
                .GetModelsAsync(cancellationToken);
        }
        catch (ClientResultException exc)
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[italic red]Incorrect endpoint or API Key: API returned {exc.Status}[/].");
            return None.Default;
        }
        catch (Exception exc) when (exc is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[italic red]Incorrect endpoint or API Key: {exc.Message}[/].");
            return None.Default;
        }

        if (models.Value.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[italic red]Incorrect endpoint or API Key: No available models found[/].");
            return None.Default;
        }

        var model = await AnsiConsole.PromptAsync(
          new SelectionPrompt<string>()
              .Title("Choose a model")
              .MoreChoicesText($"[grey](Use arrow keys to see more models ({models.Value.Count} available))[/]")
              .EnableSearch()
              .SearchPlaceholderText("Type to search models...")
              .WrapAround()
              .AddChoices(models.Value.Select(m => m.Id).OrderBy(k => k)),
          cancellationToken);

        return new LlmOptions { Endpoint = endpoint, Key = key, Model = model };
    }
}