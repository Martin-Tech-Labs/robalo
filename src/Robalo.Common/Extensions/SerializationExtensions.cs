using System.Text.Json;
using System.Text.Json.Serialization;

namespace Robalo.Common.Extensions;

public static class SerializationExtensions
{
    public static readonly JsonSerializerOptions CamelCaseOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static T? DeserializeAsCamelCase<T>(this string json) => JsonSerializer.Deserialize<T>(json, CamelCaseOptions);
    public static string SerializeAsCamelCase<T>(this T obj) => JsonSerializer.Serialize(obj, CamelCaseOptions);
}