namespace Robalo.Common.Models;

public record Created;
public record Updated;
public record NoChanges;
public record ConcurrencyConflict;
public record NotFound;
public struct Failure;
public struct Success;

public readonly union UpdateResult(Created, Updated, NoChanges, ConcurrencyConflict);
public readonly union SuccessOrNotFound(Success, NotFound);
public readonly union SuccessOrFailure(Success, Failure)
{
    public static SuccessOrFailure AsSuccess() => new Success();
    public static SuccessOrFailure AsFailure() => new Failure();
}