using System.Data;
using System.Globalization;
using Dapper;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite;

public sealed class MessageIdentifierTypeHandler : SqlMapper.TypeHandler<MessageIdentifier>
{
    public override MessageIdentifier Parse(object value)
    {
        var version = (string)value;
        return MessageIdentifier.FromIdString(version);
    }

    public override void SetValue(IDbDataParameter parameter, MessageIdentifier? value)
    {
        parameter.DbType = DbType.String;
        parameter.Value = value!.IdString;
    }

    private MessageIdentifierTypeHandler() { }
    public static readonly MessageIdentifierTypeHandler Default = new MessageIdentifierTypeHandler();
}