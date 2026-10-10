using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories;

public sealed record QueryResult(
    IReadOnlyList<Message> Messages,
    Option<Cursor> Self,
    Option<Cursor> Prev,
    Option<Cursor> Next)
{
    public static QueryResult Empty() => new([], None.Default, None.Default, None.Default);
}