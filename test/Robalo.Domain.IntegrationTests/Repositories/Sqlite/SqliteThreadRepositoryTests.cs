using Microsoft.Data.Sqlite;
using Robalo.Domain.Repositories;
using Robalo.Domain.Repositories.Sqlite;
using Serilog;

namespace Robalo.Domain.UnitTests.Repositories;

public class SqliteThreadRepositoryTests : BaseThreadRepositoryTests<SqliteThreadRepository>
{
    public SqliteThreadRepositoryTests() :
        base(new SqliteThreadRepository(Log.Logger, new SqliteConnectionFactory(Log.Logger, new("/Users/marty/git/robalo/robalo.db", ""))))
    {
    }
}