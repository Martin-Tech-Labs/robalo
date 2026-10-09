using System.Globalization;
using System.Text.RegularExpressions;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using SimpleBase;

namespace Robalo.Domain.Models;
// Messages
// 1
// 2
// 3
// 4 
// 5
// 6

// 1. 5 & 6
// Cursor for self: 2, Ref=5, AscendingIncl
// Cursor for next: N/A
// Cursor for prev: 2, Ref=5, DescendingExcl

// 2. 3 & 4
// 1. Cursor for self: 2, Ref=3, AscendingIncl
// 2. Cursor for next: 2, Ref=4, AscendingExcl
// 3. Cursor for prev: 2, Ref=3, DescendingExcl

// 3. 1 & 2
// 1. Cursor for self: 2, Ref=1, AscendingIncl
// 2. Cursor for next: 2, Ref=2, AscendingExcl
// 3. Cursor for prev: N/A

public sealed partial record Cursor
{
    public int Limit { get; }
    public int Reference { get; }
    public CursorDirection Direction { get; }
    public string CursorString { get; }

    private const string _separator = "!";
    private const string _prefix = "crs_";

    [GeneratedRegex(@"\A([0-9]+)" + _separator + @"([0-9]+)" + _separator + @"([0-9]+)\z")]
    private static partial Regex _regexId { get; }

    Cursor(int limit, int reference, CursorDirection direction, string cursorString)
    {
        Limit = limit;
        Reference = reference;
        Direction = direction;
        CursorString = cursorString;
    }

    public static Cursor Create(int limit, int reference, CursorDirection cursorDirection)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(reference, 1);

        _ = cursorDirection switch
        {
            CursorDirection.AscendingIncluding => None.Default,
            CursorDirection.AscendingExcluding => None.Default,
            CursorDirection.DescendingExcluding => None.Default,
            _ => throw new ArgumentOutOfRangeException(nameof(cursorDirection), cursorDirection, "Invalid cursor direction")
        };


        var cursorString = _prefix + Base32.Rfc4648.Encode($"{limit}{_separator}{reference}{_separator}{(int)cursorDirection}".AsBytesUtf8(), padding: false).ToLowerInvariant();

        return new(limit, reference, cursorDirection, cursorString);
    }

    public static Cursor FromCursorString(string cursorString)
    {
        if (!cursorString.StartsWith(_prefix, StringComparison.Ordinal) || cursorString.Length < _prefix.Length + 3 * 2)
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }
        var decodedCursorString = Base32.Rfc4648.Decode(cursorString.AsSpan(_prefix.Length)).ToStringUtf();

        if (!_regexId.IsMatch(decodedCursorString))
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }

        var components = decodedCursorString.Split(_separator);

        if (components.Length != 3)
        {
            throw new ArgumentException("Invalid format", nameof(cursorString));
        }

        var limit = int.Parse(components[0], NumberStyles.None, CultureInfo.InvariantCulture);
        if (limit < 1)
        {
            throw new ArgumentException("Invalid cursor: Limit must be greater than zero", nameof(cursorString));
        }

        var reference = int.Parse(components[1], NumberStyles.None, CultureInfo.InvariantCulture);
        if (reference < 1)
        {
            throw new ArgumentException("Invalid cursor: Reference must be greater than zero", nameof(cursorString));
        }

        var cursorDirection = (CursorDirection)int.Parse(components[2], NumberStyles.None, CultureInfo.InvariantCulture);
        ValidationDirection(cursorDirection);

        return new(limit, reference, cursorDirection, cursorString);
    }

    private static void ValidationDirection(CursorDirection cursorDirection)
    {
        _ = cursorDirection switch
        {
            CursorDirection.AscendingIncluding => None.Default,
            CursorDirection.AscendingExcluding => None.Default,
            CursorDirection.DescendingExcluding => None.Default,
            _ => throw new ArgumentException("Invalid cursor: Direction not supported")
        };
    }
}