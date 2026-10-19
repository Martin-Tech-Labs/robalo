using System.Diagnostics.CodeAnalysis;

namespace Robalo.Domain.Repositories.Sqlite.Configuration;

public sealed class DatabaseOptions
{
    public string? Database { get; init; }
    public string? Password { get; init; }

    public DatabaseSettings AsSettings()
    {
        Verify();
        return new(Database: Database, Password: Password);
    }

    [MemberNotNull(nameof(Database))]
    [MemberNotNull(nameof(Password))]
    public void Verify()
    {
        if (string.IsNullOrWhiteSpace(Database))
        {
            throw new InvalidOperationException("Database must be provided");
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            throw new InvalidOperationException("Password must be provided");
        }
    }
}