using System.Text.RegularExpressions;
using SimpleBase;

namespace Robalo.Domain.Models;

public closed partial class Identifier : IEquatable<Identifier>, IEquatable<string>
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

    public override bool Equals(object? obj)
    {
        if (obj is Identifier other)
        {
            return GetType() == other.GetType() &&
                StringComparer.Ordinal.Equals(IdString, other.IdString);
        }
        else if (obj is string otherString)
        {
            return StringComparer.Ordinal.Equals(IdString, obj);
        }

        return false;
    }

    public override int GetHashCode() => HashCode.Combine(GetType(), StringComparer.Ordinal.GetHashCode(IdString));

    public bool Equals(string? other) => other is not null ? StringComparer.Ordinal.Equals(IdString, other) : false;

    public bool Equals(Identifier? other) => other is not null ? StringComparer.Ordinal.Equals(IdString, other.IdString) : false;

    public static bool operator ==(Identifier left, Identifier right) => left.Equals(right);
    public static bool operator !=(Identifier left, Identifier right) => !left.Equals(right);
}