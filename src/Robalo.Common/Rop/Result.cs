using System.Drawing;

namespace Robalo.Common.Rop;
#pragma warning disable CC001

public record Exit<T>(T Value);

public readonly union Result<TNext, TResult>(TNext, TResult);

public static class Extensions
{
    public static Result<TNext, TResult> Bind<T, TNext, TResult>(this T value, Func<T, Result<TNext, TResult>> func)
    {
        return func(value);
    }

    public static Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this T value, Func<T, Task<Result<TNext, TResult>>> func)
    {
        return func(value);
    }

    public static async Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this Task<T> value, Func<T, Result<TNext, TResult>> func)
    {
        return func(await value);
    }

    public static async Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this Task<T> value, Func<T, Task<Result<TNext, TResult>>> func)
    {
        return await func(await value);
    }

    // With Result<,>

    public static async Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this Task<Result<T, TResult>> value, Func<T, Task<Result<TNext, TResult>>> func)
    {
        return await value switch
        {
            TResult res => res,
            T t => await func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static async Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this Result<T, TResult> value, Func<T, Task<Result<TNext, TResult>>> func)
    {
        return value switch
        {
            TResult res => res,
            T t => await func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static Result<TNext, TResult> Bind<T, TNext, TResult>(this Result<T, TResult> value, Func<T, Result<TNext, TResult>> func)
    {
        return value switch
        {
            TResult res => res,
            T t => func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static async Task<Result<TNext, TResult>> Bind<T, TNext, TResult>(this Task<Result<T, TResult>> value, Func<T, Result<TNext, TResult>> func)
    {
        return await value switch
        {
            TResult res => res,
            T t => func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static async Task<TResult> Map<T, TResult>(this Task<Result<T, TResult>> value, Func<T, TResult> func)
    {
        return await value switch
        {
            TResult res => res,
            T t => func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static async Task<Result<TResult, Exit<TResult>>> Map<T, TResult>(this Task<Result<T, TResult>> value, Func<T, Task<TResult>> func)
    {
        return await value switch
        {
            TResult res => new Exit<TResult>(res),
            T t => await func(t),
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }

    public static async Task<TResult> Either<TResult>(this Task<Result<TResult, Exit<TResult>>> value)
    {
        return await value switch
        {
            TResult res => res,
            Exit<TResult> exitRes => exitRes.Value,
            _ => throw new InvalidOperationException(value.GetType().Name)
        };
    }
}