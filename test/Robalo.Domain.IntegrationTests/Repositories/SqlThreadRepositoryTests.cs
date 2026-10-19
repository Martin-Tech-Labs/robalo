using Microsoft.Data.Sqlite;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.UnitTests.Repositories;

public class SqlThreadRepositoryTests : BaseThreadRepositoryTests<SqliteThreadRepository>
{
    public SqlThreadRepositoryTests() :
        base(new SqliteThreadRepository(Log.Logger, new SqlConnectionFactory(Log.Logger, new("/Users/marty/git/robalo/robalo.db", ""))))
    {
    }
}