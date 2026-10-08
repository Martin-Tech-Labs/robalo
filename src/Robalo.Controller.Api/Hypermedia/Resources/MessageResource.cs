namespace Robalo.Controller.Api.Hypermedia.Resources;

public record MessageResource(
    string Content,
    DateTimeOffset CreatedOn,
    string Source,
    string ThreadId,
    long MessageNumber,
    MessageLinks _Links) : HalResource<MessageLinks>(_Links);

public record MessageLinks(Link Self, Link Thread, Link Events) : Links(Self);