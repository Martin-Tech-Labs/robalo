using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record UserMessage(
    int Number,
    string Content,
    DateTimeOffset CreatedOn) : Message(Number, Content, CreatedOn)
{
    public static UserMessage FromUserMessageAdded(UserMessageAdded @event, int number) => new(
            Number: number,
            Content: @event.Content,
            CreatedOn: @event.AddedOn);
}