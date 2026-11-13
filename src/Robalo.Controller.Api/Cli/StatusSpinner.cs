using OpenTelemetry.Trace;
using Spectre.Console;

namespace Robalo.Controller.Api;

public class StatusSpinner(int steps)
{
    readonly int _steps = steps;
    int _number;
#pragma warning disable CC001 // Public async method should have CancellationToken parameter
    public async Task WithLabel(string text, Func<Task> action)
#pragma warning restore CC001 // Public async method should have CancellationToken parameter
    {
        var label = $"[bold]{++_number}/{_steps}[/] {text}";
        try
        {
            if (AnsiConsole.Console.Profile.Capabilities.Interactive)
            {
                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync(label, _ => action());
            }
            else
            {
                await action();
            }

            AnsiConsole.MarkupLine($"{label} .. [bold green]OK[/]");
        }
        catch
        {
            AnsiConsole.MarkupLine($"{label} .. [bold red]FAIL[/]");
            throw;
        }
    }
}