using System.Data;
using System.Data.Common;

namespace Robalo.Domain.Repositories;

public interface ISqlConnectionFactory
{
    public Task<TResult> Execute<TResult>(
        Func<DbConnection, Task<TResult>> configure,
        CancellationToken cancellationToken);
}