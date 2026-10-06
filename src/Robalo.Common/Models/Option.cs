namespace Robalo.Common.Models;

public record None;
public record Some<T>(T Value);

public readonly union Option<T>(None, Some<T>);