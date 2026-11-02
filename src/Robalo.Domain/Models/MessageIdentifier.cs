namespace Robalo.Domain.Models;

public sealed class MessageIdentifier : Identifier
{
    protected override string Prefix => "msg_";

    private MessageIdentifier(string id) : base(id) { }

    private MessageIdentifier(Guid uuid) : base(uuid) { }

    public static implicit operator string(MessageIdentifier value) => value.ToString();

    public static MessageIdentifier FromIdString(string id) => new(id);

    public static MessageIdentifier FromUuid(Guid uuid) => new(uuid);

    public static MessageIdentifier NewIdentifier() => FromUuid(Guid.NewGuid());
}