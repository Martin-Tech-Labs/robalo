using System.Diagnostics.CodeAnalysis;

namespace Robalo.Domain.Repositories.Sqlite.Configuration;

public sealed class DatabaseOptions
{
    public string? Database { get; set; }
    public string? Password { get; set; }

    public DatabaseSettings AsSettings()
    {
        Verify();
        if(!Path.Exists(Database))
        {
            throw new ArgumentException($"Database {Path.GetFullPath(Database)} does not exist");
        }
        return new(
            Database: Database,
            Password: Password);

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