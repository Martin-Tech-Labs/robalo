using System.Text.Json.Serialization;

namespace Robalo.Controller.Api.Hypermedia;

public record Link(
    Uri Href, [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool Templated = false);