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