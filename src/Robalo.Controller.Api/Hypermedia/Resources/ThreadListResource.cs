using System.Text.Json.Serialization;

namespace Robalo.Controller.Api.Hypermedia.Resources;

public record ThreadListResource(
    [property: JsonPropertyName("_embedded")]
    ThreadListEmbedded Embedded,
    ThreadListLinks Links) : HalResource<ThreadListLinks>(Links);

public record ThreadListLinks(
    Link Self) : Links(Self);


public record ThreadListEmbedded(List<ThreadResource> Threads);