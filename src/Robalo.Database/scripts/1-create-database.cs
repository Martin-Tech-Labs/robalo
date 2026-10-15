#!/usr/bin/env dotnet

#:package Spectre.Console@0.57.2
#:package Microsoft.Data.Sqlite.Core@11.0.0-rc.2.26504.105
#:package SQLite3MC.PCLRaw.bundle@2.6.1

using Microsoft.Data.Sqlite;
using Spectre.Console;

var database = args.FirstOrDefault() is { Length: > 0 } filename ? filename : "robalo.db";

if (!Path.HasExtension(database))
{
    database += ".db";
}

if (Path.Exists(database))
{
    AnsiConsole.MarkupLineInterpolated($"[bold red]Error! File {database} already exists[/].");
    return 1;
}

var migrationsPath = "src/Robalo.Database/migrations";

var migrations = Directory.GetFiles(migrationsPath, "*.sql", SearchOption.AllDirectories)
    .Order(StringComparer.Ordinal)
    .ToArray();

if (migrations.Length == 0)
{
    AnsiConsole.MarkupLineInterpolated($"[bold red]Error! No scripts found in {migrationsPath}[/].");
    return 1;
}

List<char> passwordChars = [];

Console.WriteLine("Enter a passphrase and press <Enter>. If no password is entered, a non-encrypted database will be created.");
ConsoleKeyInfo key;
while ((key = Console.ReadKey(true)).Key != ConsoleKey.Enter)
{
    if (key.Key == ConsoleKey.Backspace && passwordChars.Count > 0)
    {
        passwordChars.RemoveAt(passwordChars.Count - 1);
        Console.Write("\b \b"); // Remove the last asterisk
    }
    else if (passwordChars.Count == 0 && char.IsWhiteSpace(key.KeyChar))
    {
        // Ignore whitespace at the beginning
    }
    else if (passwordChars.Count > 0 && char.IsWhiteSpace(key.KeyChar) && char.IsWhiteSpace(passwordChars[^1]))
    {
        // Ignore subsequent whitespaces
    }
    else if (!char.IsControl(key.KeyChar))
    {
        passwordChars.Add(key.KeyChar);
        Console.Write("*"); // Display asterisk for each character
    }
}

Console.WriteLine();

var password = new string([.. passwordChars]);

if (!string.IsNullOrWhiteSpace(password) && (password.Length < 8 || char.IsWhiteSpace(passwordChars[^1])))
{
    AnsiConsole.MarkupLineInterpolated($"[bold red]Error! If provided, password must contain at least 8 characters and not end with a whitespace[/].");
    return 1;
}

SQLitePCL.Batteries_V2.Init();

var connectionStringBuilder = new SqliteConnectionStringBuilder
{
    DataSource = database
};

if (!string.IsNullOrWhiteSpace(password))
{
    connectionStringBuilder.Password = password;
}

await using var connection = new SqliteConnection(connectionStringBuilder.ToString());

await connection.OpenAsync();

var i = 1;
foreach (var migration in migrations)
{
    using var command = connection.CreateCommand();
    command.CommandText = File.ReadAllText(migration);
    var label = $"{i++}/{migrations.Length} Executing script {migration}";
    await AnsiConsole.Status()
       .Spinner(Spinner.Known.Dots)
       .StartAsync(label, _ => command.ExecuteNonQueryAsync());

    Console.WriteLine($"{label} .. OK");
}

using var command2 = connection.CreateCommand();
command2.CommandText = "SELECT sqlite_version(),sqlite3mc_version();";
var reader = await command2.ExecuteReaderAsync();
await reader.ReadAsync();
var version1 = reader.GetValue(0);
var version2 = reader.GetValue(1);


if (!string.IsNullOrWhiteSpace(password))
{
    AnsiConsole.MarkupLineInterpolated($"[bold green]Success! 🔑 Encrypted database {database} was created[/] with Sqlite [yellow]{version1}[/] ({version2})");
}
else
{
    AnsiConsole.MarkupLineInterpolated($"[bold yellow] Non-encrypted database {database} was created[/] with Sqlite [yellow]{version1}[/] ({version2})");
}
return 0;