#:package SimpleBase@5.6.4

using System.Security.Cryptography;
using SimpleBase;

Console.WriteLine("Hello");

Guid id = Guid.NewGuid();

Console.WriteLine(id.ToString("N"));
Console.WriteLine(id.ToString("N").Length);


//26 characters, no "=" padding
string encoded = Base32.Rfc4648.Encode(id.ToByteArray(), padding: false).ToLowerInvariant();

Console.WriteLine(encoded);

// Decode back to the original GUID
Guid decoded = new Guid(Base32.Rfc4648.Decode(encoded));

Console.WriteLine(decoded.ToString("N"));

Console.WriteLine(new Option<int>(new Some<int>(23)).ToString());
Console.WriteLine(new Option<int>(new None()).ToString());

Option<int> d = default;
Option<int> d2 = new Some<int>(4);
Option<int> d3 = new None();
Console.WriteLine(d);

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
        _  => "None"
    };

}