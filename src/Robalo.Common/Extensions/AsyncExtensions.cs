using System.Runtime.CompilerServices;

namespace Robalo.Common.Extensions;

public static class AsyncExtensions
{
    public static async IAsyncEnumerable<TOut> Map<TIn, TOut>(
        this IAsyncEnumerable<TIn> source,
        Func<TIn, TOut> transform,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return transform(item);
        }
    }
}