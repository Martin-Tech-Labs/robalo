using System.Globalization;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Robalo.Common.Services;
using Robalo.Domain.Configuration;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api.Cli;

public static class CliApp
{
#pragma warning disable CC001 // Public async method should have CancellationToken parameter
    public static async Task<int> Run(string[] args)
#pragma warning restore CC001 // Public async method should have CancellationToken parameter
    {
        var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
        {
            Args = args,
            ContentRootPath = AppContext.BaseDirectory
        });

        builder.Configuration.AddJsonFile(
            "appsettings.cli.json", optional: false, reloadOnChange: false);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.WithProperty("Version", Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.ToString() ?? string.Empty)
            .CreateLogger();

        builder.Services.AddSerilog(Log.Logger);
        builder.Services.AddSingleton<IDateTimeOffsetProvider, DateTimeOffsetProvider>();
        builder.Services.AddOptions();

        builder.Services
            .AddOptions<DatabaseOptions>()
            .Bind(builder.Configuration.GetSection("Database"));

        var title = new FigletText("ROBALO").Color(Color.Cyan1);

        AnsiConsole.Write(
            new Panel(title)
                .Border(BoxBorder.Rounded)
                .BorderColor(Color.Cyan1)
                .Padding(1, 0));

        var cancellationTokenSource = new CancellationTokenSource();

        // Wire up Console.CancelKeyPress to trigger cancellation
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true; // Prevent immediate process termination
            cancellationTokenSource.Cancel();
            AnsiConsole.MarkupLineInterpolated(CultureInfo.InvariantCulture, $"[italic yellow]Cancelling...[/]");
        };

        var app = new CommandApp(new TypeRegistrar(builder.Services));
        app.Configure(config =>
        {
            config.CancellationExitCode(Exitcodes.Canceled);
            config.PropagateExceptions();
            // Configure parsing behavior
            config.Settings.CaseSensitivity = CaseSensitivity.None;

            config.SetApplicationName("robalo");
            config.SetApplicationVersion(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.ToString() ?? string.Empty);
            config.AddCommand<InitCommand>("init")
                .WithDescription("Initialize Robalo application")
                .WithAlias("i")
                .WithExample("init")
                .WithExample("init -f data/robalo.db");

            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Modify configuration of Robalo application")
                .WithAlias("c");
        });

        try
        {
            return await app.RunAsync(args, cancellationTokenSource.Token);
        }
        catch (CliException exc)
        {
            AnsiConsole.MarkupLineInterpolated(
                CultureInfo.InvariantCulture,
                $"[bold red]Error![/] {exc.Message}");

            return exc.ExitCode;
        }
        catch (OperationCanceledException) when (cancellationTokenSource.IsCancellationRequested)
        {
            AnsiConsole.MarkupLineInterpolated(
                CultureInfo.InvariantCulture,
                $"[bold yellow]Command canceled.[/]");

            return Exitcodes.Canceled;
        }
        catch (Exception exc)
        {
            Log.Logger.Error(exc, "Unexpected error occurred");
            AnsiConsole.MarkupLineInterpolated(
                CultureInfo.InvariantCulture,
                $"[bold white on red]Oops, something went horribly wrong![/] [italic red]{exc.Message}[/]");
            
            return Exitcodes.UnexpectedError;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}