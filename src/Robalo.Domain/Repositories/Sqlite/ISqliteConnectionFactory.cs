using System.Data;
using System.Data.Common;

namespace Robalo.Domain.Repositories.Sqlite;

public interface ISqliteConnectionFactory
{
    public Task<TResult> Execute<TResult>(
        Func<DbConnection, Task<TResult>> configure,
        CancellationToken cancellationToken);
}