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

    public static Link LinkToThreads(this IHttpContextAccessor accessor)
         => accessor.HttpContext!.LinkToPath($"/threads");

    public static Link LinkToThreadId(this IHttpContextAccessor accessor, string threadId)
        => accessor.HttpContext!.LinkToPath($"/threads/{threadId}");

    public static Link LinkToMessageNumber(
        this IHttpContextAccessor accessor,
        ThreadIdentifier threadId,
        int messageNumber)
            => accessor.HttpContext!.LinkToPath($"/threads/{threadId}/messages/{messageNumber}");

    public static Link LinkToMessages(
        this IHttpContextAccessor accessor,
        ThreadIdentifier threadId)
            => accessor.HttpContext!.LinkToPath($"/threads/{threadId}/messages");

    public static Link LinkToCursor(
        this IHttpContextAccessor accessor,
        ThreadIdentifier threadId,
        Cursor cursor)
            => accessor.HttpContext!.LinkToPath($"/threads/{threadId}/messages/query/{cursor}");


    public static Link LinkToEventsWithThreadId(
        this IHttpContextAccessor accessor,
        ThreadIdentifier threadId)
            => accessor.HttpContext!.LinkToPath($"/events?thread_id={threadId}");

    public static Link LinkToEvents(this IHttpContextAccessor accessor)
            => accessor.HttpContext!.LinkToPath($"/events");

    public static Link LinkToServiceDiscovery(this IHttpContextAccessor accessor)
        => accessor.HttpContext!.LinkToPath($"");
}