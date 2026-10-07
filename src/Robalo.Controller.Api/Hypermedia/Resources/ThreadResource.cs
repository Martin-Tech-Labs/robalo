namespace Robalo.Controller.Api.Hypermedia.Resources;

public record ThreadResource(
    DateTimeOffset CreatedOn,
    DateTimeOffset ModifiedOn,
    string? Title,
    string ThreadId,
    ThreadLinks _Links) : HalResource<ThreadLinks>(_Links);

public record ThreadLinks(Link Self, Link Messages, Link Events) : Links(Self);