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
    }

    public static Option<T> AsOption<T>(this None none) where T : notnull => none;
    public static Option<T> Some<T>(this T value) where T : notnull => value;

#pragma warning disable CC001
    public static async Task<Option<T>> Some<T>(this Task<T> task) where T : notnull
    {
        return (await task).Some();
    }
#pragma warning restore


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

    public static T2 Match<T1, T2>(this Option<T1> value, Func<T1, T2> someFunc, Func<T2> noneFunc) where T1 : notnull =>
        value switch
        {
            Some<T1> some => someFunc(some.Value),
            None => noneFunc()
        };

    public static T ValueOr<T>(this Option<T> value, T alternativeValue) where T : notnull =>
        value switch
        {
            Some<T> some => some,
            _ => alternativeValue
        };

    public static string ValueOrString<T>(this Option<T> value, string alternativeValue) where T : notnull =>
        value switch
        {
            Some<T> some => some.Value.ToString() ?? "",
            _ => alternativeValue
        };

    public static Option<T> SingleOrNone<T>(this IEnumerable<T> enumerable) where T : notnull =>
        enumerable.Any() ? enumerable.Single() : None.Default;

    public static Option<T2> SingleOrNone<T1, T2>(this IEnumerable<T1> enumerable, Func<T1, T2> map)
        where T1 : notnull
        where T2 : notnull => enumerable.Select(map).SingleOrNone();
}