using System.Text.RegularExpressions;
using SimpleBase;

namespace Robalo.Domain;

public sealed partial class Identifier : IEquatable<Identifier>
{
    public string Id { get; }
    public Guid Uuid { get; }
    public string Prefix { get; }

    [GeneratedRegex(@"\A[a-z]{3}_[a-z2-7]{26}\z")]
    private static partial Regex _regexId { get; }

    [GeneratedRegex(@"\A[a-z]{3}\z")]
    private static partial Regex _regexPrefix { get; }

    private Identifier(string id, Guid uuid, string prefix)
    {
        Id = id;
        Uuid = uuid;
        Prefix = prefix;
    }

    public static Identifier FromId(string? id)
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

        return new(
            id: id,
            uuid: uuid,
            prefix: id[..3]);
    }

    public static Identifier FromUuid(string? prefix, Guid uuid)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(prefix, nameof(prefix));

        if (!_regexPrefix.IsMatch(prefix))
        {
            throw new ArgumentException("Wrong format", nameof(prefix));
        }

        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(uuid));
        }

        return new(
            id: $"{prefix}_{Base32.Rfc4648.Encode(bytes: uuid.ToByteArray(), padding: false).ToLowerInvariant()}",
            uuid: uuid,
            prefix: prefix);
    }

    public override string ToString() => Id;

    public bool Equals(Identifier? other) => other?.Id == Id;

    public override bool Equals(object? obj) => obj is Identifier other && other.Id == Id;

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);
}