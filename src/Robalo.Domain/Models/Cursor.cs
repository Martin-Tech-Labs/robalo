using System.Globalization;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using SimpleBase;

namespace Robalo.Domain.Models;

public sealed partial record Cursor
{
    public int Limit { get; }
    public Option<int> Reference { get; }
    public string CursorString { get; }

    private const string _separator = "!";
    private const string _prefix = "crs_";
    private const int _maxLimit = 10_000;

    Cursor(int limit, Option<int> reference, string cursorString)
    {
        Limit = limit;
        Reference = reference;
        CursorString = cursorString;
    }

    public static Cursor Create(int limit, int reference)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, _maxLimit);
        ArgumentOutOfRangeException.ThrowIfLessThan(reference, 1);

        var cursorString = _prefix + Base32.Rfc4648.Encode($"{limit}{_separator}{reference}".AsBytesUtf8(), padding: false).ToLowerInvariant();
        return new(limit, reference, cursorString);
    }

    public static Cursor Last(int limit)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, _maxLimit);

        var cursorString = _prefix + Base32.Rfc4648.Encode($"{limit}".AsBytesUtf8(), padding: false).ToLowerInvariant();
        return new(limit, None.Default, cursorString);
    }

    public static Cursor First(int limit) => Create(limit: limit, reference: 1);

    public static Cursor FromCursorString(string cursorString)
    {
        if (!cursorString.StartsWith(_prefix, StringComparison.Ordinal) || cursorString.Length < _prefix.Length + 1)
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }
        var decodedCursorString = Base32.Rfc4648.Decode(cursorString.AsSpan(_prefix.Length)).ToStringUtf();

        var components = decodedCursorString.Split(_separator);

        if (components.Length == 0 || components.Length > 2)
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }

        var limit = int.Parse(components[0], NumberStyles.None, CultureInfo.InvariantCulture);
        if (limit < 1 && limit > _maxLimit)
        {
            throw new ArgumentException($"Invalid cursor: Limit must be greater than zero and less than {_maxLimit}", nameof(cursorString));
        }

        if (components.Length == 1)
        {
            return new(limit, None.Default, cursorString);
        }

        var reference = int.Parse(components[1], NumberStyles.None, CultureInfo.InvariantCulture);
        if (reference < 1)
        {
            throw new ArgumentException("Invalid cursor: Reference must be greater than zero", nameof(cursorString));
        }

        return new(limit, reference, cursorString);
    }
}