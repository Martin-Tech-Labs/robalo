using System.Diagnostics;
using Microsoft.Extensions.Options;
using Robalo.Common.Models;

namespace Robalo.Common.Configuration;

public sealed class Smart<T, TOptions> : ISmart<T>, IDisposable
    where T : notnull
    where TOptions : class
{
    readonly IDisposable? _onChange;
    Lazy<T> _value;
    public T Value => Volatile.Read(ref _value).Value;
    public Smart(IOptionsMonitor<TOptions> optionsMonitor, Func<TOptions, T> valueFactory)
    {
        _value = new Lazy<T>(() => valueFactory(optionsMonitor.CurrentValue));
        _onChange = optionsMonitor.OnChange((_, _) =>
        {
            Volatile.Write(ref _value, new Lazy<T>(() => valueFactory(optionsMonitor.CurrentValue)));
        });
    }

    public void Dispose()
    {
        _onChange?.Dispose();
        GC.SuppressFinalize(this);
    }
}