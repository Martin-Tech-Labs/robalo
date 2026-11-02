using System.Diagnostics;

namespace Robalo.Domain.IntegrationTests;

public class DatabaseFixture : IDisposable
{
    readonly Lock _lock = new();
    public DatabaseFixture()
    {
        Database = $"{Guid.NewGuid():N}.db";
        Password = $"{Guid.NewGuid():N}";
        CreateRepository(Database, Password);
    }

    private void CreateRepository(string database, string password)
    {
        lock (_lock)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(database);
            ArgumentException.ThrowIfNullOrWhiteSpace(password);

            File.Delete(database);

            using var process = Process.Start(new ProcessStartInfo("./src/Robalo.Database/scripts/1-create-database.cs")
            {
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                ArgumentList =
        {
            "--",
            database
        }
            });

            process!.StandardInput.WriteLine(password);
            process.StandardInput.Close();
            process.WaitForExit();
            process.ExitCode.ShouldBe(0, $"{process.StandardOutput.ReadToEnd()}+{process.StandardError.ReadToEnd()}");
        }
    }

    public void Dispose()
    {
        File.Delete(Database);
        GC.SuppressFinalize(this);
    }

    public string Database { get; }

    public string Password { get; }
}
