using System.Diagnostics.CodeAnalysis;

namespace Robalo.Domain.Repositories.Sqlite.Configuration;

public sealed class DatabaseOptions
{
    public string? FilePath { get; set; }

    public DatabaseSettings AsSettings()
    {
        Verify();
        var password = Environment.GetEnvironmentVariable("ROBALO_PASSWORD");
        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length != password.Length || password.Length < 8)
        {
            throw new InvalidOperationException("Missing or invalid password");
        }
        return new(FilePath, password);
    }

    [MemberNotNull(nameof(FilePath))]
    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(FilePath))
        {
            throw new InvalidOperationException("Database file must be provided");
        }

        if (!File.Exists(FilePath))
        {
            throw new InvalidOperationException($"Database {Path.GetFullPath(FilePath)} does not exist");
        }
    }
}