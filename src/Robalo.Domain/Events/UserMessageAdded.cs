namespace Robalo.Domain.Events;

public record UserMessageAdded(Identifier Id, string Content, DateTimeOffset AddedOn);