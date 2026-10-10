using System.Globalization;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using SimpleBase;

namespace Robalo.Domain.Models;

public sealed partial record Cursor
{
    public int Limit { get; }
    public Option<int> OverrideLimit { get; }
    public Option<int> Reference { get; }
    public string CursorString { get; }

    private const string _separator = "!";
    private const string _prefix = "crs_";
    private const string _none = "none";
    private const int _maxLimit = 10_000;

    Cursor(int limit, Option<int> referenceOrNone, Option<int> overrideLimitOrNone)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(limit, _maxLimit);

        overrideLimitOrNone.DoIfSome(overrideLimit => ArgumentOutOfRangeException.ThrowIfGreaterThan(overrideLimit, limit));
        overrideLimitOrNone.DoIfSome(overrideLimit => ArgumentOutOfRangeException.ThrowIfLessThan(overrideLimit, 1));
        referenceOrNone.DoIfSome(reference => ArgumentOutOfRangeException.ThrowIfLessThan(reference, 1));

        Limit = limit;
        Reference = referenceOrNone;
        OverrideLimit = overrideLimitOrNone;

        CursorString = _prefix + Base32.Rfc4648.Encode(
           $"{Limit}{_separator}{Reference.ValueOrString("none")}{_separator}{overrideLimitOrNone.ValueOrString("none")}".AsBytesUtf8(), padding: false).ToLowerInvariant();
    }

    public Cursor WithOneOffLimitOverride(int overrideLimit) =>
        new(limit: Limit, referenceOrNone: Reference, overrideLimitOrNone: overrideLimit);

    public static Cursor Create(int limit, int reference) =>
        new(limit, reference, None.Default);

    public static Cursor Last(int limit) =>
         new(limit, None.Default, None.Default);

    public static Cursor First(int limit) => Create(limit: limit, reference: 1);

    public static implicit operator string(Cursor value) => value.ToString();

    public static Cursor FromCursorString(string cursorString)
    {
        if (!cursorString.StartsWith(_prefix, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }
        var decodedCursorString = Base32.Rfc4648.Decode(cursorString.AsSpan(_prefix.Length)).ToStringUtf();

        var components = decodedCursorString.Split(_separator);

        if (components.Length != 3)
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }

        var limit = int.Parse(components[0], NumberStyles.None, CultureInfo.InvariantCulture);
        var reference = (components[1] == _none) ? None.Default : int.Parse(components[1], NumberStyles.None, CultureInfo.InvariantCulture).Some();
        var overrideLimit = (components[2] == _none) ? None.Default : int.Parse(components[2], NumberStyles.None, CultureInfo.InvariantCulture).Some();

        return new(limit: limit, referenceOrNone: reference, overrideLimitOrNone: overrideLimit);
    }

    public override string ToString() => CursorString;
}