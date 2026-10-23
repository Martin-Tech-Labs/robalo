using System.Text.RegularExpressions;
using SimpleBase;

namespace Robalo.Domain.Models;

public closed partial class Identifier
{
    public string IdString { get; }
    public Guid Uuid { get; }

    [GeneratedRegex(@"\A[a-z2-7]{26}\z")]
    private static partial Regex _regexId { get; }

    protected abstract string Prefix { get; }

    protected Identifier(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);

        var prefixWithSeparator = $"{Prefix}";
        if (!id.StartsWith(prefixWithSeparator, StringComparison.Ordinal)
            || !_regexId.IsMatch(id[prefixWithSeparator.Length..]))
        {
            throw new ArgumentException("Wrong format", nameof(id));
        }

        var uuid = new Guid(Base32.Rfc4648.Decode(id.AsSpan(prefixWithSeparator.Length)));
        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(id));
        }

        IdString = id;
        Uuid = uuid;
    }

    protected Identifier(Guid uuid)
    {
        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(uuid));
        }

        IdString = $"{Prefix}{Base32.Rfc4648.Encode(bytes: uuid.ToByteArray(), padding: false).ToLowerInvariant()}";
        Uuid = uuid;
    }

    public override string ToString() => IdString;

    public override bool Equals(object? obj) =>
        obj is Identifier other
        && GetType() == other.GetType()
        && StringComparer.Ordinal.Equals(IdString, other.IdString);

    public override int GetHashCode() => HashCode.Combine(GetType(), StringComparer.Ordinal.GetHashCode(IdString));
}