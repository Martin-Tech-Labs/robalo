namespace Robalo.Domain.Models;

public sealed class MessageIdentifier : Identifier, IEquatable<MessageIdentifier>
{
    protected override string Prefix => "msg_";

    private MessageIdentifier(string id) : base(id) { }

    private MessageIdentifier(Guid uuid) : base(uuid) { }

    public static implicit operator string(MessageIdentifier value) => value.ToString();

    public static MessageIdentifier FromIdString(string id) => new(id);

    public static MessageIdentifier FromUuid(Guid uuid) => new(uuid);

    public static MessageIdentifier NewIdentifier() => FromUuid(Guid.NewGuid());

    public bool Equals(MessageIdentifier? other) => base.Equals(other);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(MessageIdentifier id1, MessageIdentifier id2) => id1.Equals(id2);
    public static bool operator !=(MessageIdentifier id1, MessageIdentifier id2) => !id1.Equals(id2);
}