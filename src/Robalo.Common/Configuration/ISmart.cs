namespace Robalo.Common.Configuration;

public interface ISmart<T> where T : notnull
{
    public T Value { get; }
}
