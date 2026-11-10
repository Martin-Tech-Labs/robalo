using System.ComponentModel;
using System.Reflection;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Robalo.Controller.Api;

public class InitCommand : AsyncCommand<InitCommand.Settings>
{
    public class Settings : CommandSettings
    {
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var migrations = Assembly.GetEntryAssembly()!.GetManifestResourceNames()
            .Where(name => name.StartsWith("Robalo.Database.Migrations.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal);

        foreach (var name in migrations)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await using var stream = Assembly.GetEntryAssembly()!.GetManifestResourceStream(name);
            using var reader = new StreamReader(stream!);
            var sql = await reader.ReadToEndAsync(cancellationToken);
            Console.WriteLine(sql);
        }
        return 0;
    }
}