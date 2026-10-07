using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace Robalo.Controller.Api.Hypermedia;

public static class Extensions
{
    public static Link AsLink(this Uri uri) => new(Href: uri, Templated: false);
    public static Link LinkToPath(this HttpContext context, string path)
    {
        var baseUri = new Uri(UriHelper.BuildAbsolute(
            scheme: context.Request.Scheme,
            host: context.Request.Host,
            pathBase: context.Request.PathBase,
            path: path));

        return baseUri.AsLink();
    }
}