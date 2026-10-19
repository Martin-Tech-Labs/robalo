using System.Diagnostics.CodeAnalysis;

namespace Robalo.Domain.Repositories.Sqlite.Configuration;

public sealed class DatabaseOptions
{
    public string? Database { get; set; }
    public string? Password { get; set; }

    public DatabaseSettings AsSettings()
    {
        Verify();
        var db = Path.HasExtension(Database) ? Database : Path.ChangeExtension(Database, ".db");
        if(!Path.Exists(db))
        {
            throw new ArgumentException($"Database {db} does not exist, full path {Path.GetFullPath(db)}");
        }


        return new(
            Database: db,
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