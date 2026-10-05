namespace Robalo.Domain.Models;

public record Message(
    Identifier Id,
    MessageSource Source,
    string Content,
    DateTimeOffset CreatedOn);