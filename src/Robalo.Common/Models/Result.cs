namespace Robalo.Common.Models;

public record Created;
public record Updated;
public record NoChanges;
public record ConcurrencyConflict;
public record NotFound;
public record Success;

public readonly union UpdateResult(Created, Updated, NoChanges, ConcurrencyConflict);
public readonly union SuccessOrNotFound(Success, NotFound);