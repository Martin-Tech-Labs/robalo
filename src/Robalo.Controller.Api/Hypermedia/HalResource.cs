using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace Robalo.Controller.Api.Hypermedia;

public abstract record HalResource<TLinks>(
    [property: JsonPropertyName("_links")] TLinks Links) where TLinks : Links
{
    public CreatedResult AsCreated() => new(Links.Self.Href.AbsoluteUri, this);
    public OkObjectResult AsOk() => new(this);
}