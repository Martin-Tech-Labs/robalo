using System.Text.Json.Serialization;
using Robalo.Domain.Configuration;

namespace Robalo.Controller.Api.Hypermedia.Resources;

public record ConfigurationResource(
    int ConfigurationId,
    DateTimeOffset CreatedOn,
    RobaloOptions Data,
    ConfigurationLinks Links) : HalResource<ConfigurationLinks>(Links);

public record ConfigurationLinks(Link Self) : Links(Self);