namespace Robalo.Common.Extensions;

public static class LambdaExtensions
{
    public static IEnumerable<T2> MapEach<T1, T2>(this IEnumerable<T1> items, Func<T1, T2> map)
    {
        foreach (var item in items)
        {
            yield return map(item);
        }
    }
    
    public static T1 ForEach<T1, T2>(this T1 value, IEnumerable<T2> items, Action<T1, T2> action)
        where T1 : notnull
        where T2 : notnull
    {
        foreach (var item in items)
        {
            action(value, item);
        }
        return value;
    }

    public static T1 DoIf<T1, T2>(this T1 value1, T2 value2, Predicate<T2> predicate, Action<T1, T2> action)
        where T1 : notnull
    {
        if (predicate(value2))
        {
            action(value1, value2);
        }
        return value1;
    }

    public static T Do<T>(this T value, Action<T> action)
      where T : notnull
    {
        action(value);
        return value;
    }

    public static T1 DoIfNot<T1, T2>(this T1 value1, T2 value2, Predicate<T2> predicate, Action<T1, T2> action)
        where T1 : notnull => value1.DoIf(value2, t => !predicate(t), action);

    public static T DoIfNotNullOrWhiteSpace<T>(
        this T value,
        string? text,
        Action<T, string> action)
        where T : notnull
    {
        if (!string.IsNullOrWhiteSpace(text))
            action(value, text);

        return value;
    }
}