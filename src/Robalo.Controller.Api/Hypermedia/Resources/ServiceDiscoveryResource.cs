using System.Text.Json.Serialization;

namespace Robalo.Controller.Api.Hypermedia.Resources;


public record ServiceDiscoveryResource(ServiceDiscoveryLinks Links)
    : HalResource<ServiceDiscoveryLinks>(Links);

public record ServiceDiscoveryLinks(
    Link Self,
    Link Threads,
    Link Events) : Links(Self);