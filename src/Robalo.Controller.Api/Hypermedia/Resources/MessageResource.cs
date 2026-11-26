namespace Robalo.Controller.Api.Hypermedia.Resources;


public record MessageResource(
    string MessageId,
    string Content,
    DateTimeOffset CreatedOn,
    string EventType,
    string ThreadId,
    long MessageNumber,
    string? RepliedToMessageId,
    int? PromptTokens,
    int? Tokens,
    string? Model,
    MessageLinks Links) : HalResource<MessageLinks>(Links);

public record MessageLinks(Link Self, Link Thread, Link? RepliedTo, Link Events) : Links(Self);