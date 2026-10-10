using Microsoft.AspNetCore.Http.Extensions;
using Robalo.Domain.Models;

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
            path: "/"));

        return new Uri(baseUri, path.TrimStart('/')).AsLink();
    }

    public static Link LinkToThreadId(this HttpContext context, string threadId)
        => context.LinkToPath($"/threads/{threadId}");

    public static Link LinkToMessageNumber(
        this HttpContext context,
        ThreadIdentifier threadId,
        int messageNumber)
            => context.LinkToPath($"/threads/{threadId}/messages/{messageNumber}");

    public static Link LinkToMessages(
        this HttpContext context,
        ThreadIdentifier threadId)
            => context.LinkToPath($"/threads/{threadId}/messages");

    public static Link LinkToCursor(
        this HttpContext context,
        ThreadIdentifier threadId,
        Cursor cursor)
            => context.LinkToPath($"/threads/{threadId}/messages/query/{cursor}");


    public static Link LinkToEventsWithThreadId(
        this HttpContext context,
        ThreadIdentifier threadId)
            => context.LinkToPath($"/events?thread_id={threadId}");
}