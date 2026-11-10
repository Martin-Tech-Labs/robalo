using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api.Cli;

public static class CliApp
{
    public static Task<int> Run(string[] args)
    {
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
            Console.WriteLine("Cancellation requested...");
        };

        var app = new CommandApp();
        app.Configure(config =>
        {
            // Configure parsing behavior
            config.Settings.CaseSensitivity = CaseSensitivity.None;

            config.SetApplicationName("robalo");
            config.SetApplicationVersion(Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.ToString() ?? string.Empty);
            config.AddCommand<InitCommand>("init")
                .WithDescription("Initialize Robalo application")
                .WithAlias("i")
                .WithExample("init");

            config.AddCommand<ConfigCommand>("config")
                .WithDescription("Modify configuration of Robalo application")
                .WithAlias("c");
        });
        return app.RunAsync(args, cancellationTokenSource.Token);
    }
}