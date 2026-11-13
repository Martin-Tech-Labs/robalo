using System.Globalization;
using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Options;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Services;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api.Cli.Commands;

public class ConfigCommand : AsyncCommand<ConfigCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    readonly ILogger _logger;
    readonly IDateTimeOffsetProvider _dateTimeOffsetProvider;
    readonly DatabaseOptions _options;

    public ConfigCommand(ILogger logger, IDateTimeOffsetProvider dateTimeOffsetProvider, IOptions<DatabaseOptions> options)
    {
        _logger = logger.ForContext(GetType());
        _dateTimeOffsetProvider = dateTimeOffsetProvider;
        _options = options.Value;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.FilePath) || !File.Exists(_options.FilePath))
        {
            throw new CliException(Exitcodes.CommandNotSupported, "Application not initialized. Use init command instead");
        }

        var password = await new TextPrompt<string>("Specify a master password to proceed:")
                         .Secret()
                         .ShowAsync(AnsiConsole.Console, cancellationToken);

        if (password.Length < 8)
        {
            throw new CliException(Exitcodes.InputError, "Invalid password");
        }

        var databaseSettings = new DatabaseSettings(_options.FilePath, password);
        Extensions.AddDapperTypeHandlers();
        var configurationRepository = new SqliteConfigurationRepository(Log.Logger, new SqliteConnectionFactory(Log.Logger, databaseSettings));

        var currentConfig = await GetCurrentConfiguration();
        currentConfig.Value.Verify();
        
        var llmOptionsOrNone = await Helper.GetLlmOptionsInteractive(cancellationToken, currentConfig.Value.LlmOptions);

        if (llmOptionsOrNone is not Some<LlmOptions> llmOptions)
        {
            throw new CliException(Exitcodes.InputError, "Wrong values provided");
        }

        var robaloOptions = new RobaloOptions(llmOptions);
        robaloOptions.Verify();

        await configurationRepository.SaveConfiguration(robaloOptions, _dateTimeOffsetProvider.UtcNow, cancellationToken);

        AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[bold green]Success! Configuration has been updated and will shortly take effect. No restart is necessary.[/]");

        return Exitcodes.Success;

        async Task<RobaloConfiguration> GetCurrentConfiguration()
        {
            try
            {
                return (await configurationRepository.LoadConfiguration(cancellationToken)).ValueOrFailure;
            }
            catch (SqliteException ex) when (ex.SqliteErrorCode == 26)
            {
                throw new CliException(Exitcodes.InputError, "Invalid password");
            }
        }
    }
}