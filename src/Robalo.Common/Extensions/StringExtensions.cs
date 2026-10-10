using System.Text;

namespace Robalo.Common.Extensions;

public static class StringExtensions
{
    public static byte[] AsBytesUtf8(this string value) => Encoding.UTF8.GetBytes(value);

    public static string ToStringUtf(this byte[] bytes) => Encoding.UTF8.GetString(bytes);
}