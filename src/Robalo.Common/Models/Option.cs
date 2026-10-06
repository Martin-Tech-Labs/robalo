using System.Diagnostics.CodeAnalysis;

namespace Robalo.Common.Models;

public record None
{
    public override string ToString() => nameof(None);
}
public record Some<T>(T Value) where T : notnull
{
    public static implicit operator Some<T>(T value) => new(value);
    public static implicit operator T(Some<T> value) => value.Value;

    public override string ToString() => $"Some({Value})";
}

public readonly union Option<T>(None, Some<T>) where T : notnull
{
    public static implicit operator Option<T>(T value) => new Some<T>(value);

    public static bool operator ==(Option<T> option, T value) =>
    option switch
    {
        Some<T> some => EqualityComparer<T>.Default.Equals(some.Value, value),
        _ => false
    };
    public static bool operator ==(T value, Option<T> option) =>
    option == value;

    public static bool operator !=(Option<T> option, T value) => !(option == value);
    public static bool operator !=(T value, Option<T> option) => !(option == value);

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is Option<T> other)
        {
            return other switch
            {
                Some<T> someOther => this switch
                {
                    Some<T> thisSome => EqualityComparer<T>.Default.Equals(thisSome.Value, someOther.Value),
                    _ => false
                },
                _ => this is None
            };
        }

        return false;
    }

    public override int GetHashCode() => this switch
    {
        Some<T> some => EqualityComparer<T>.Default.GetHashCode(some.Value),
        _ => 0
    };
    
    public override string ToString() => this switch
    {
        Some<T> some => some.ToString(),
        None none => none.ToString()
    };
}

public static class OptionExtensions
{
    public static Option<T> Some<T>(this T value) where T : notnull => new Some<T>(value);
}