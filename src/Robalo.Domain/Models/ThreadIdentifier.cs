using System.Text.RegularExpressions;
using Robalo.Common.Models;
using SimpleBase;

namespace Robalo.Domain.Models;

public sealed partial class ThreadIdentifier : IEquatable<ThreadIdentifier>
{
    private const string ThreadIdPrefix = "trd";
    public string IdString { get; }
    public Guid Uuid { get; }

    [GeneratedRegex(@"\A" + ThreadIdPrefix + @"_[a-z2-7]{26}\z")]
    private static partial Regex _regexId { get; }

    ThreadIdentifier(string id, Guid uuid)
    {
        IdString = id;
        Uuid = uuid;
    }

    public static implicit operator string(ThreadIdentifier value) => value.ToString();


    public static Option<ThreadIdentifier> TryGetFromId(string id)
    {
        try
        {
            return FromIdString(id);
        }
        catch (ArgumentException)
        {
            return None.Default;
        }
    }

    public static ThreadIdentifier FromIdString(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        if (!_regexId.IsMatch(id))
        {
            throw new ArgumentException("Wrong format", nameof(id));
        }

        var uuid = new Guid(Base32.Rfc4648.Decode(id.AsSpan()[4..]));
        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(id));
        }

        return new(id: id, uuid: uuid);
    }

    public static ThreadIdentifier FromUuid(Guid uuid)
    {
        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(uuid));
        }

        return new(
            id: $"{ThreadIdPrefix}_{Base32.Rfc4648.Encode(bytes: uuid.ToByteArray(), padding: false).ToLowerInvariant()}",
            uuid: uuid);
    }

    public static ThreadIdentifier NewIdentifier() => FromUuid(Guid.NewGuid());

    public override string ToString() => IdString;

    public bool Equals(ThreadIdentifier? other) => other?.IdString == IdString;

    public override bool Equals(object? obj) => obj is ThreadIdentifier other && other.Equals(this);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(IdString);
}