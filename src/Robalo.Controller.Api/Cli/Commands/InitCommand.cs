using System;
using System.ClientModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Hangfire.States;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Files;
using OpenAI.Models;
using Robalo.Common.Services;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-f|--file")]
        [Description("Database file name")]
        [DefaultValue("data/robalo.db")]
        public required string File { get; init; }
    }

    readonly ILogger _logger;
    readonly IDateTimeOffsetProvider _dateTimeOffsetProvider;
    readonly DatabaseOptions _options;

    public InitCommand(ILogger logger, IDateTimeOffsetProvider dateTimeOffsetProvider, IOptions<DatabaseOptions> options)
    {
        _logger = logger.ForContext(GetType());
        _options = options.Value;
        _dateTimeOffsetProvider = dateTimeOffsetProvider;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_options.Database) && File.Exists(_options.Database))
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold yellow]Robalo is already configured with database '{Path.GetFullPath(_options.Database)}'. Command aborted.[/]");
            return 1;
        }

        var databaseFullPath = Path.ChangeExtension(Path.GetFullPath(settings.File), ".db");

        if (databaseFullPath != settings.File
            && !await AnsiConsole.ConfirmAsync($"The initialization will create database file at '{databaseFullPath}'?", true, cancellationToken))
        {
            return 1;
        }

        if (File.Exists(databaseFullPath))
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold yellow]Database file '{databaseFullPath}' already exists. Cannot modify existing database. Command aborted.[/]");
            return 1;
        }

        string password = await GetPassword(cancellationToken);

        var endpointChoices = new Dictionary<string, Uri>
        {
            ["Ollama locally (http://localhost:11500/v1/)"] =
            new("http://localhost:11500/v1/"),
            ["OpenAI (https://api.openai.com/v1/)"] =
            new("https://api.openai.com/v1/"),
            ["Anthropic (https://api.anthropic.com/v1)"] =
            new("https://api.anthropic.com/v1")
        };

        const string custom = "Custom";

        var choice = await AnsiConsole.PromptAsync(
            new SelectionPrompt<string>()
                .Title("Choose an LLM endpoint")
                .AddChoices(endpointChoices.Keys.Append(custom)),
            cancellationToken);

        var endpoint = choice == custom
            ? await AnsiConsole.PromptAsync(
                new TextPrompt<Uri>("Enter a custom OpenAI-compatible LLM endpoint")
                    .Validate(
                        uri => uri.IsAbsoluteUri &&
                            (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps),
                        "[red]Enter an absolute HTTP(S) URI.[/]"),
                cancellationToken)
            : endpointChoices[choice];

        var key = await AnsiConsole.PromptAsync(new TextPrompt<string>("Enter an API Key").Secret(), cancellationToken);

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
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold red]Error! Incorrect endpoint or API Key: API returned {exc.Status}[/].");
            return 1;
        }
        catch (Exception exc) when (exc is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold red]Error! Incorrect endpoint or API Key: {exc.Message}[/].");
            return 1;
        }

        if (models.Value.Count == 0)
        {
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold red]Error! Incorrect endpoint or API Key: No available models found[/].");
            return 1;
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

        var migrations = Assembly.GetEntryAssembly()!.GetManifestResourceNames()
            .Where(name => name.StartsWith("Robalo.Database.Migrations.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        var options = new RobaloOptions(new LlmOptions { Endpoint = endpoint, Key = key, Model = model });
        options.Verify();

        SQLitePCL.Batteries_V2.Init();

        _ = Path.GetDirectoryName(databaseFullPath) switch
        {
            string directory => Directory.CreateDirectory(directory),
            _ => default
        };

        var configurationRepository = new SqliteConfigurationRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new DatabaseSettings(databaseFullPath, password)));

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databaseFullPath,
            Password = password,
        }.ToString());

        try
        {
            await connection.OpenAsync(cancellationToken);
            await CreateAndConfigureDatabase();
        }
        catch (Exception exc) when (exc is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            _logger.Error(exc, "Command failed during database creation and configuration");
            File.Delete(databaseFullPath);
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold red]Something went wrong during the setup[/].");
            return 1;
        }

        return 0;

        static async Task<string> GetPassword(CancellationToken cancellationToken)
        {
            var password = await new TextPrompt<string>("Specify a master password (min 8 characters):")
                .Validate(v => v.Length >= 8, "Should contain at least 8 characters")
                .Secret()
                .ShowAsync(AnsiConsole.Console, cancellationToken);

            await new TextPrompt<string>("Confirm the master password:")
               .Validate(v => v == password, "Passwords must match")
               .Secret()
               .ShowAsync(AnsiConsole.Console, cancellationToken);
            return password;
        }

        async Task CreateAndConfigureDatabase()
        {
            var i = 1;
            foreach (var migration in migrations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(migration);
                using var reader = new StreamReader(stream!);

                await using var command = connection.CreateCommand();
                command.CommandText = await reader.ReadToEndAsync(cancellationToken);
                var label = $"{i++}/{migrations.Count} Executing script {migration}";
                await AnsiConsole.Status()
                   .Spinner(Spinner.Known.Dots)
                   .StartAsync(label, _ => command.ExecuteNonQueryAsync(cancellationToken));

                Console.WriteLine($"{label} .. OK");
            }

            await configurationRepository.SaveConfiguration(options, _dateTimeOffsetProvider.UtcNow, cancellationToken);
            var versionCommand = connection.CreateCommand();
            versionCommand.CommandText = "SELECT sqlite_version(),sqlite3mc_version();";
            var readerSqlVersion = await versionCommand.ExecuteReaderAsync(cancellationToken);
            await readerSqlVersion.ReadAsync(cancellationToken);
            var version1 = readerSqlVersion.GetValue(0);
            var version2 = readerSqlVersion.GetValue(1);

            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold green]Success! 🔑 Encrypted database {databaseFullPath} was created and configured to use with Robalo[/]. Sqlite [yellow]{version1}[/] ({version2})");
            return;
        }
    }
}