using Robalo.Common.Models;

namespace Robalo.Common.Extensions;

public static class OptionExtensions
{
    extension<T>(Option<T> option) where T : notnull
    {
        public T ValueOrFailure => option switch
        {
            Some<T> some => some.Value,
            _ => throw new InvalidOperationException("Cannot get Value for None")
        };

#pragma warning disable CA1000 // Do not declare static members on generic types
        public static Option<T> None => None.Default;
#pragma warning restore CA1000 // Do not declare static members on generic types
    }

    public static Option<T> AsOption<T>(this T? value) where T : notnull =>
        value is null ? None.Default : value;

    public static Option<T> AsOption<T>(this T? value) where T : struct =>
        value is null ? None.Default : value.Value;

    public static Option<T> Some<T>(this T value) where T : notnull => value;

#pragma warning disable CC001
    public static async Task<Option<T>> Some<T>(this Task<T> task) where T : notnull
    {
        return (await task).Some();
    }
#pragma warning restore

    public static Option<T> SomeIf<T>(this T? valueOrNull, Func<T, bool> predicate) where T : struct
    {
        if (valueOrNull is T value)
        {
            return predicate(value) ? value : None.Default;
        }

        return None.Default;
    }

    public static T2? FuncOrDefault<T1, T2>(this Option<T1> value, Func<T1, T2> func) where T1 : notnull =>
         value switch
         {
             Some<T1> some => func(some.Value),
             _ => default
         };

    public static void DoIfSome<T>(this Option<T> value, Action<T> action) where T : notnull
    {
        if (!value.HasValue)
        {
            return;
        }

        action(value.ValueOrFailure);
    }

    public static T ValueOr<T>(this Option<T> value, T alternativeValue) where T : notnull =>
        value switch
        {
            Some<T> some => some,
            _ => alternativeValue
        };

    public static T? ToNullable<T>(this Option<T> value) where T : notnull =>
        value switch
        {
            Some<T> some => some.Value,
            _ => default
        };


    public static string ValueOrString<T>(this Option<T> value, string alternativeValue) where T : notnull =>
        value switch
        {
            Some<T> some => some.Value.ToString() ?? "",
            _ => alternativeValue
        };

    public static Option<T> SingleOrNone<T>(this IEnumerable<T> enumerable) where T : notnull =>
        enumerable.Any() ? enumerable.Single() : None.Default;

#pragma warning disable CC001 // Public async method should have CancellationToken parameter
    public async static Task<Option<T>> SingleOrNone<T>(this Task<IEnumerable<T>> enumerable) where T : notnull =>
      (await enumerable).SingleOrNone();
#pragma warning restore CC001 // Public async method should have CancellationToken parameter

    public static Option<T2> SingleOrNone<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> map)
        where T1 : notnull
        where T2 : notnull => enumerable.Select(map).SingleOrNone();
}