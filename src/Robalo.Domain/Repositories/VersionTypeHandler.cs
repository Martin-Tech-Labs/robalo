using System.Data;
using System.Globalization;
using Dapper;

namespace Robalo.Domain.Repositories;

public sealed class VersionTypeHandler : SqlMapper.TypeHandler<Version>
{
    public override Version Parse(object value)
    {
        var version = (string)value;
        return Version.FromVersionString(version);
    }

    public override void SetValue(IDbDataParameter parameter, Version? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value!.VersionString;
    }

    private VersionTypeHandler() { }
    public static readonly VersionTypeHandler Default = new VersionTypeHandler();
}