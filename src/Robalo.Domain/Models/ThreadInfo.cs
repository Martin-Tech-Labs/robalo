namespace Robalo.Domain.Models;

public sealed record ThreadInfo(
    ThreadIdentifier Id, 
    string? Title, 
    DateTimeOffset CreatedOn, 
    DateTimeOffset ModifiedOn,
    int Version);