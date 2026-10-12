using System.Text.Json.Serialization;

namespace Robalo.Controller.Api.Hypermedia.Resources;

public record MessageQueryResource(
    [property: JsonPropertyName("_embedded")]
    MessageQueryEmbedded Embedded,
    MessageQueryLinks Links) : HalResource<MessageQueryLinks>(Links);

public record MessageQueryLinks(
    Link Self,
    Link Thread,
    Link Events,
    Link? Next,
    Link? Prev,
    Link First,
    Link Last) : Links(Self);


public record MessageQueryEmbedded(List<MessageResource> Messages);