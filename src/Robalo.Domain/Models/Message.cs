using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record Message(
    long Number,
    MessageSource Source,
    string Content,
    DateTimeOffset CreatedOn)
{
    public static Message FromUserMessageAdded(UserMessageAdded @event, int number) => new(
            Number: number,
            Source: MessageSource.User,
            Content: @event.Content,
            CreatedOn: @event.AddedOn);
}