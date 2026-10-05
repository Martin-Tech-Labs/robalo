using System.Text.RegularExpressions;
using SimpleBase;

namespace Robalo.Domain.Models;

public sealed partial class Identifier : IEquatable<Identifier>
{
    public enum IdentifierType
    {
        Thread, Message
    }

    private const string ThreadIdPrefix = "trd";
    private const string MessageIdPrefix = "msg";

    public string Id { get; }
    public Guid Uuid { get; }
    public IdentifierType Type { get; }

    [GeneratedRegex(@"\A[a-z]{3}_[a-z2-7]{26}\z")]
    private static partial Regex _regexId { get; }

    Identifier(string id, Guid uuid, IdentifierType type)
    {
        Id = id;
        Uuid = uuid;
        Type = type;
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

        var type = id[..3] switch
        {
            ThreadIdPrefix => IdentifierType.Thread,
            MessageIdPrefix => IdentifierType.Message,
            _ => throw new ArgumentException("Unknown prefix", nameof(id))
        };

        return new(
            id: id,
            uuid: uuid,
            type: type);
    }

    public static Identifier ThreadIdFromUuid(Guid guid) => FromUuid(ThreadIdPrefix, guid, IdentifierType.Thread);
    public static Identifier MessageIdFromUuid(Guid guid) => FromUuid(MessageIdPrefix, guid, IdentifierType.Message);

    static Identifier FromUuid(string prefix, Guid uuid, IdentifierType type)
    {
        if (uuid == Guid.Empty)
        {
            throw new ArgumentException("Empty value", nameof(uuid));
        }

        return new(
            id: $"{prefix}_{Base32.Rfc4648.Encode(bytes: uuid.ToByteArray(), padding: false).ToLowerInvariant()}",
            uuid: uuid,
            type: type);
    }


    public static Identifier NewThreadId() => NewIdentifier(ThreadIdPrefix, Guid.NewGuid(), IdentifierType.Thread);
    public static Identifier NewMessageId() => NewIdentifier(MessageIdPrefix, Guid.NewGuid(), IdentifierType.Message);

    static Identifier NewIdentifier(string prefix, Guid guid, IdentifierType type) => new(
          id: $"{prefix}_{Base32.Rfc4648.Encode(bytes: guid.ToByteArray(), padding: false).ToLowerInvariant()}",
          uuid: guid,
          type: type);

    public override string ToString() => Id;

    public bool Equals(Identifier? other) => other?.Id == Id;

    public override bool Equals(object? obj) => obj is Identifier other && other.Equals(this);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Id);
}