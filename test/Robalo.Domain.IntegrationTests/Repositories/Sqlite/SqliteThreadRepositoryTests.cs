using System.Diagnostics;
using Microsoft.Data.Sqlite;
using Robalo.Domain.IntegrationTests;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Serilog;

namespace Robalo.Domain.UnitTests.Repositories;

public class SqliteThreadRepositoryTests : BaseThreadRepositoryTests<SqliteThreadRepository>, IClassFixture<DatabaseFixture>
{
    public SqliteThreadRepositoryTests(DatabaseFixture fixture) : base(new SqliteThreadRepository(Log.Logger, new SqliteConnectionFactory(Log.Logger, new(fixture.Database, fixture.Password))))
    {
    }
}