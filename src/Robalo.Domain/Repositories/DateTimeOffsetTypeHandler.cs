using System.Data;
using System.Globalization;
using Dapper;

namespace Robalo.Domain.Repositories;

public sealed class DateTimeOffsetTypeHandler : SqlMapper.TypeHandler<DateTimeOffset>
{
    public override DateTimeOffset Parse(object value)
    {
        var date = (string)value;
        return DateTimeOffset.ParseExact(date, "o", CultureInfo.InvariantCulture);
    }

    public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value.ToString("o", CultureInfo.InvariantCulture);
    }

    private DateTimeOffsetTypeHandler() { }
    public static readonly DateTimeOffsetTypeHandler Default = new DateTimeOffsetTypeHandler();
}