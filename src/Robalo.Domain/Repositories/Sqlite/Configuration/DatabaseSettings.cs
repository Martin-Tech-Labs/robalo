namespace Robalo.Domain.Repositories.Sqlite.Configuration;

public record DatabaseSettings(string Database, string Password)
{
    public const string DefaultFilePath = "data/robalo.db";
    public static DatabaseSettings Create() => Create(DefaultFilePath);
    public static DatabaseSettings Create(string filePath)
    {
        var databaseFullPath = Path.ChangeExtension(Path.GetFullPath(filePath, AppContext.BaseDirectory), ".db");

        if (!File.Exists(databaseFullPath))
        {
            throw new ArgumentException($"Database {databaseFullPath} does not exist", nameof(filePath));
        }

        var password = Environment.GetEnvironmentVariable("ROBALO_PASSWORD");
        if (string.IsNullOrWhiteSpace(password) || password.Trim().Length != password.Length || password.Length < 8)
        {
            throw new InvalidOperationException("Missing or invalid password");
        }

        return new(databaseFullPath, password);
    }
}