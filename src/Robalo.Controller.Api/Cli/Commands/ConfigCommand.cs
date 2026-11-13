using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api;

public class ConfigCommand : AsyncCommand<ConfigCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        Console.WriteLine("not yet supported");
        return 0;
    }
}