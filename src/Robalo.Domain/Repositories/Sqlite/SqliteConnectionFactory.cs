using System.Data;
using System.Data.Common;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Serilog;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class SqliteConnectionFactory : ISqliteConnectionFactory
{
    readonly DatabaseSettings _databaseSettings;
    readonly ILogger _logger;

    public SqliteConnectionFactory(ILogger logger, DatabaseSettings databaseSettings)
    {
        _logger = logger.ForContext(GetType());
        _databaseSettings = databaseSettings;
    }

    public async Task<TResult> Execute<TResult>(
        Func<DbConnection, Task<TResult>> configure,
        CancellationToken cancellationToken)
    {
        await using var connection = new SqliteConnection
          (new SqliteConnectionStringBuilder
          {
              DataSource = _databaseSettings.Database,
              Password = _databaseSettings.Password,
              Mode = SqliteOpenMode.ReadWrite
          }.ToString());
        await connection.OpenAsync(cancellationToken);
        return await configure(connection);
    }
}