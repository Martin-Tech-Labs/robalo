using System.Data;
using System.Globalization;
using Dapper;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class ThreadIdentifierTypeHandler : SqlMapper.TypeHandler<ThreadIdentifier>
{
    public override ThreadIdentifier Parse(object value)
    {
        var version = (string)value;
        return ThreadIdentifier.FromIdString(version);
    }

    public override void SetValue(IDbDataParameter parameter, ThreadIdentifier? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value!.IdString;
    }

    private ThreadIdentifierTypeHandler() { }
    public static readonly ThreadIdentifierTypeHandler Default = new ThreadIdentifierTypeHandler();
}