namespace Robalo.Domain;

public record Message(
    Identifier Id,
    MessageSource Source,
    string Content,
    DateTimeOffset CreatedOn);