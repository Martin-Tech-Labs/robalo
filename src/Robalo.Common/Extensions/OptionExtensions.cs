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
}