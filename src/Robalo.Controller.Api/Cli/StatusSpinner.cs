using OpenTelemetry.Trace;
using Spectre.Console;

namespace Robalo.Controller.Api;

public class StatusSpinner(int steps)
{
    public enum Status { OK, SKIPPED, FAIL };
    readonly int _steps = steps;
    int _number;
#pragma warning disable CC001 // Public async method should have CancellationToken parameter
    public async Task WithLabel(string text, Func<Task<Status>> action)
#pragma warning restore CC001 // Public async method should have CancellationToken parameter
    {
        var label = $"[bold]{++_number}/{_steps}[/] {text}";
        var status = Status.FAIL;
        try
        {
            if (AnsiConsole.Console.Profile.Capabilities.Interactive)
            {
                status = await AnsiConsole.Status()
                     .Spinner(Spinner.Known.Dots)
                     .StartAsync(label, _ => action());
            }
            else
            {
                status = await action();
            }

            var finalLabel = status switch
            {
                Status.OK => "[bold green]OK[/]",
                Status.SKIPPED => "[bold cyan]SKIPPED[/]",
                Status.FAIL => "[bold red]FAIL[/]",
                _ => throw new InvalidOperationException($"Unknown status {status}")
            };
            AnsiConsole.MarkupLine($"{label} .. {finalLabel}");
        }
        catch
        {
            AnsiConsole.MarkupLine($"{label} .. [bold red]FAIL[/]");
            throw;
        }
    }
    public Task WithLabel(string text, Func<Task> action)
        => WithLabel(text, async () =>
        {
            await action();
            return Status.OK;
        });
}