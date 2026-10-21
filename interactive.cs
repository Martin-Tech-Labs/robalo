#!/usr/bin/env dotnet


#:package SimpleBase@5.6.4

using System.Globalization;
using System.Security.Cryptography;
using SimpleBase;
var utc = DateTimeOffset.UtcNow.ToString("o");


List<int> list = [1, 2, 3, 4, 5, 6, 9, 11, 12, 44, 45, 46];

Convert(list);


void Convert(string str)
{
    var ranges = str.Split(',').Select(s => s.Split('-')).Select(s => new NumberRange(int.Parse(s[0]), int.Parse(s[1])));

    foreach (var range in ranges)
    {
        yield return range.ToNumbers();
    }
}

void Convert(List<int> numbers)
{
    if (numbers.Count == 0)
    {
        return;
    }
    var sorted = numbers.Order().ToList();
    List<NumberRange> ranges = [];

    var range = new NumberRange(sorted[0], sorted[0]);
    foreach (var number in sorted.Skip(1))
    {
        if (number == range.To + 1)
        {
            range = range with { To = number };
        }
        else
        {
            ranges.Add(range);
            range = new(number, number);
        }
    }
    ranges.Add(range);

    if (ranges.Count == 1)
    {
        Console.WriteLine(ranges[0].ToString());
        return;
    }


    var res = ranges[1..^0].Aggregate(ranges[0].ToString(), (a, r) => a += $",{r}");
    Console.WriteLine(res);

}

public record NumberRange(int From, int To)
{
    public override string ToString()
    {
        if (From == To) return $"{From}";
        return $"{From}-{To}";
    }
    public IEnumerable<int> ToNumbers()
    {
       return Enumerable.Range(From, To - From + 1);
    }
}

public struct None
{
    public override string ToString() => nameof(None);
};
public record Some<T>(T Value) where T : notnull
{
    public static implicit operator Some<T>(T value) => new(value);
    public static implicit operator T(Some<T> value) => value;
    public override string ToString() => $"Some({Value})";
}

public readonly union Option<T>(None, Some<T>) where T : notnull
{
    public override string ToString() => this switch
    {
        Some<T> some => some.ToString(),
        _ => "None"
    };

}