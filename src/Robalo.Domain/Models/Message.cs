using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record Message(
    Identifier Id,
    MessageSource Source,
    string Content,
    DateTimeOffset CreatedOn)
{
    public static Message FromUserMessageAdded(UserMessageAdded @event) => new(
            Id: @event.Id,
            Source: MessageSource.User,
            Content: @event.Content,
            CreatedOn: @event.AddedOn);
}