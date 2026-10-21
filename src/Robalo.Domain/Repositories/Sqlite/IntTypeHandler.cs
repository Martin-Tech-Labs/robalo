using System.Data;
using System.Globalization;
using Dapper;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class IntTypeHandler : SqlMapper.TypeHandler<int>
{
    public override int Parse(object value)
    {
        var val = (long)value;
        return (int)val;
    }

    public override void SetValue(IDbDataParameter parameter, int value)
    {
        parameter.DbType = DbType.Int64;
        parameter.Value = value;
    }

    private IntTypeHandler() { }
    public static readonly IntTypeHandler Default = new IntTypeHandler();
}