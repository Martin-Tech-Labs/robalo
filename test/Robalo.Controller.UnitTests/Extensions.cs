using System.Reflection;
using Robalo.Controller.Api.Hypermedia;

namespace Robalo.Controller.UnitTests;

public static class Extensions
{
    public static void ValidateAllNotNullLinks<TLinks>(
        this HalResource<TLinks> resource,
        string scheme,
        string host,
        int port,
        string pathBase) where TLinks : Links
    {
        resource.Links.ShouldNotBeNull();

        var context = new NullabilityInfoContext();

        var links = resource.Links
            .GetType()
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.PropertyType == typeof(Link) && context.Create(p).ReadState == NullabilityState.NotNull)
            .Select(p => p.GetValue(resource.Links))
            .ToList();

        if (links is null || links.Count == 0)
        {
            throw new InvalidOperationException("No links found on the supplied object");
        }

        foreach (Link link in links!)
        {
            link.ShouldNotBeNull();
            link.Templated.ShouldBeFalse();
            link.Href.ShouldNotBeNull();
            link.Href.Scheme.ShouldBe(scheme);
            link.Href.Host.ShouldBe(host);
            link.Href.Port.ShouldBe(port);
            link.Href.Segments[1].ShouldBe($"{pathBase}/");
        }
    }
}

