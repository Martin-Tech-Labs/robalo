using System;
using System.ClientModel;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Hangfire.States;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Files;
using OpenAI.Models;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Common.Services;
using Robalo.Controller.Api.Cli;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Extensions = Robalo.Domain.Repositories.Sqlite.Extensions;

namespace Robalo.Controller.Api;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("-f|--file")]
        [Description("Database file name")]
        [DefaultValue("data/robalo.db")]
        public required string File { get; init; }

        [CommandOption("-n|--non-interactive [VALUE]")]
        [Description("Non-interactive mode")]
        [DefaultValue(true)]
        public required FlagValue<bool> NonInteractive { get; init; }

        [CommandOption("-y|--yes [VALUE]")]
        [Description("Automatic confirmation")]
        [DefaultValue(true)]
        public required FlagValue<bool> AutoConfirm { get; init; }
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
        if (!string.IsNullOrWhiteSpace(_options.FilePath) && File.Exists(_options.FilePath))
        {
            throw new CliException(
                Exitcodes.CommandNotSupported,
                $"Robalo is already configured with database file '{Path.GetFullPath(_options.FilePath)}'");
        }

        var databaseFullPath = Path.ChangeExtension(Path.GetFullPath(settings.File, AppContext.BaseDirectory), ".db");

        if (File.Exists(databaseFullPath))
        {
            throw new CliException(
                Exitcodes.InputError,
                $"Database file '{databaseFullPath}' already exists. Cannot modify existing database.");
        }

        if (databaseFullPath != settings.File && !settings.AutoConfirm.Value && settings.NonInteractive.Value)
        {
            throw new CliException(
                Exitcodes.InputError,
                "Provide absolute database file path with .db extension or specify --yes flag.");
        }

        if (!settings.AutoConfirm.Value && databaseFullPath != settings.File)
        {
            if (!await AnsiConsole.ConfirmAsync($"The initialization will create database file at '{databaseFullPath}'. Proceed? (Note: database file must always have extension '.db')", true, cancellationToken))
            {
                throw new CliException(
                    Exitcodes.ConfirmationRefused,
                    "Confirmation refused.");
            }
        }

        var password = await GetPassword();
        var options = await GetConfiguration();

        var migrations = Assembly.GetEntryAssembly()!.GetManifestResourceNames()
            .Where(name => name.StartsWith("Robalo.Database.Migrations.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .ToList();

        if (migrations.Count == 0)
        {
            throw new InvalidOperationException("No migrations found");
        }

        SQLitePCL.Batteries_V2.Init();

        _ = Path.GetDirectoryName(databaseFullPath) switch
        {
            string directory => Directory.CreateDirectory(directory),
            _ => default
        };

        var configurationRepository = new SqliteConfigurationRepository(
            Log.Logger,
            new SqliteConnectionFactory(Log.Logger, new DatabaseSettings(databaseFullPath, password)));

        Extensions.AddDapperTypeHandlers();

        await using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = databaseFullPath,
            Password = password,
        }.ToString());

        try
        {
            await connection.OpenAsync(cancellationToken);
            await ConfigureRobalo();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            await connection.DisposeAsync();
            File.Delete(databaseFullPath);
            throw;
        }
        catch (Exception exc) when (!cancellationToken.IsCancellationRequested || exc is not OperationCanceledException)
        {
            await connection.DisposeAsync();
            File.Delete(databaseFullPath);
            throw;
        }

        return Exitcodes.Success;

        async Task<string> GetPassword()
        {
            return settings.NonInteractive.Value ?
                await GetPassword_NonInteractive() : await GetPassword_Interactive();
        }

        async Task<string> GetPassword_Interactive()
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

        static async Task<string> GetPassword_NonInteractive()
        {
            var password = Environment.GetEnvironmentVariable("ROBALO_PASSWORD");
            if (string.IsNullOrWhiteSpace(password) || password.Trim().Length != password.Length || password.Length < 8)
            {
                throw new CliException(Exitcodes.InputError, "Invalid or missing password.");
            }
            return password;
        }

        async Task ConfigureRobalo()
        {
            var statusSpinner = new StatusSpinner(migrations.Count + 4);

            string _getVersionsQuery = "SELECT sqlite_version(),sqlite3mc_version();";

            foreach (var migration in migrations)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(migration);
                using var reader = new StreamReader(stream!);

                await statusSpinner.WithLabel($"Executing script {migration}", async () =>
                {
                    await using var command = connection.CreateCommand();
                    command.CommandText = await reader.ReadToEndAsync(cancellationToken);
                    await command.ExecuteNonQueryAsync(cancellationToken);
                });
            }

            await statusSpinner.WithLabel("Saving configuration",
                async () => await configurationRepository.SaveConfiguration(options, _dateTimeOffsetProvider.UtcNow, cancellationToken));

            string version1 = "", version2 = "";

            await statusSpinner.WithLabel("Checking database metadata", async () =>
            {
                var versionCommand = connection.CreateCommand();
                versionCommand.CommandText = _getVersionsQuery;
                var readerSqlVersion = await versionCommand.ExecuteReaderAsync(cancellationToken);
                await readerSqlVersion.ReadAsync(cancellationToken);
                version1 = (string)readerSqlVersion.GetValue(0);
                version2 = (string)readerSqlVersion.GetValue(1);
            });

            await statusSpinner.WithLabel("Verifying database encryption", async () =>
            {
                await using var connectionPlain = new SqliteConnection(new SqliteConnectionStringBuilder
                {
                    DataSource = databaseFullPath
                }.ToString());

                await connectionPlain.OpenAsync(cancellationToken);

                await using var command3 = connectionPlain.CreateCommand();
                command3.CommandText = _getVersionsQuery;
                var isThrown = false;

                try
                {
                    await command3.ExecuteReaderAsync(cancellationToken);
                }
                catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
                {
                    isThrown = true;
                }

                if (!isThrown)
                {
                    throw new InvalidOperationException("Could not create an encrypted database: encryption failed");
                }
            });

            await statusSpinner.WithLabel("Wrapping up setup", async () => await UpdateAppSettings());

            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold green]Success! 🔑 Encrypted database {databaseFullPath} was created and configured to use with Robalo[/]. Sqlite [yellow]{version1}[/] ({version2})");
        }

        async Task UpdateAppSettings()
        {
            var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
            var root = JsonNode.Parse(await File.ReadAllTextAsync(appSettingsPath, cancellationToken))!.AsObject();

            var database = root["Database"] as JsonObject ?? new JsonObject();
            root["Database"] = database;
            database["FilePath"] = databaseFullPath;

            await File.WriteAllTextAsync(
                appSettingsPath,
                root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }),
                cancellationToken);
        }

        async Task<RobaloOptions> GetRobaloOptions_NonInteractive()
        {
            var json = Environment.GetEnvironmentVariable("ROBALO_CONFIG");
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new CliException(Exitcodes.InputError, "Missing configuration");
            }

            RobaloOptions robaloOptions;
            try
            {
                robaloOptions = json.DeserializeAsCamelCase<RobaloOptions>() ?? throw new CliException(Exitcodes.InputError, "Invalid configuration: parsing error.");
            }
            catch (JsonException exc)
            {
                throw new CliException(Exitcodes.InputError, $"Invalid configuration: parsing error. Details: {exc.Message}");
            }

            try
            {
                robaloOptions.Verify();
            }
            catch (Exception exc) when (exc is not OperationCanceledException)
            {
                throw new CliException(Exitcodes.InputError, $"Invalid configuration: {exc.Message}");
            }

            var client = new OpenAIClient(
                new ApiKeyCredential(robaloOptions.LlmOptions.Key!),
                new OpenAIClientOptions { Endpoint = robaloOptions.LlmOptions.Endpoint });

            OpenAIModelCollection models;

            try
            {
                models = await client
                    .GetOpenAIModelClient()
                    .GetModelsAsync(cancellationToken);
            }
            catch (ClientResultException exc)
            {
                throw new CliException(Exitcodes.InputError, $"Incorrect endpoint or API Key: API returned {exc.Status}");
            }
            catch (Exception exc) when (exc is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
            {
                throw new CliException(Exitcodes.InputError, $"Incorrect endpoint or API Key: {exc.Message}");
            }

            if (!models.Any(m => m.Id == robaloOptions.LlmOptions.Model))
            {
                throw new CliException(Exitcodes.InputError, "Incorrect model name.");
            }

            return robaloOptions;
        }

        async Task<RobaloOptions> GetConfiguration()
        {
            return (settings.NonInteractive.Value) ?
                await GetRobaloOptions_NonInteractive() :
                await GetRobaloOptions_Interactive();
        }

        async Task<RobaloOptions> GetRobaloOptions_Interactive()
        {
            Option<LlmOptions> llmOptionsOrNone = None.Default;
            do
            {
                cancellationToken.ThrowIfCancellationRequested();
                llmOptionsOrNone = await GetLlmOptions_Interactive();
            }
            while (llmOptionsOrNone.IsNone);

            var robaloOptions = new RobaloOptions(llmOptionsOrNone.ValueOrFailure);
            return robaloOptions.Verify();
        }

        async Task<Option<LlmOptions>> GetLlmOptions_Interactive()
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
}