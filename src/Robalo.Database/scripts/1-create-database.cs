#!/usr/bin/env dotnet

#:package Spectre.Console@0.57.2
#:package Microsoft.Data.Sqlite.Core@11.0.0-rc.2.26504.105
#:package SQLitePCLRaw.bundle_e_sqlite3@3.0.5

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

SQLitePCL.Batteries_V2.Init();

using var connection = new SqliteConnection(new SqliteConnectionStringBuilder
{
    DataSource = database
}.ToString());
connection.Open();


var i = 1;
foreach (var migration in migrations)
{
    using var command = connection.CreateCommand();
    command.CommandText = File.ReadAllText(migration);
    var label = $"{i++}/{migrations.Length} Executing script {migration}";
    AnsiConsole.Status()
       .Spinner(Spinner.Known.Dots)
       .Start(label, _ => command.ExecuteNonQuery());

    Console.WriteLine($"{label} .. OK");
}

AnsiConsole.MarkupLineInterpolated($"[bold green]Success! Database {database} was created[/].");

return 0;