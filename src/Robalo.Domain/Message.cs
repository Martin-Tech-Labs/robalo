namespace Robalo.Domain;

public record Message(Identifier Id, string Content, DateTimeOffset CreatedOn);