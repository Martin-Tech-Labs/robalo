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
}