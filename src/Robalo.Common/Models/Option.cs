using System.Diagnostics.CodeAnalysis;

namespace Robalo.Common.Models;

public struct None
{
    public static None Default => new();
}

public record Some<T>(T Value) where T : notnull
{
    public static implicit operator Some<T>(T value) => new(value);
    public static implicit operator T(Some<T> value) => value.Value;

    public override string ToString() => $"Some({Value})";
}

public readonly union Option<T>(None, Some<T>) where T : notnull
{
    public bool HasValue => this is Some<T>;
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

    public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);

    public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

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
                _ => this is None || Value is null
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
        _ => nameof(None)
    };
}