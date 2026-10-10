using Robalo.Domain.Models;

namespace Robalo.Domain.Events;

/// <summary>
/// Occurrs when a user adds a message to a thread (so-called prompt)
/// </summary>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record UserMessageAdded(
    string Content,
    DateTimeOffset AddedOn)
{
    public static UserMessageAdded FromMessage(Message message) => new(
        Content: message.Content,
        AddedOn: message.CreatedOn);
}