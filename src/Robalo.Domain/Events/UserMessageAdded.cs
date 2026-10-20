using Robalo.Domain.Models;

namespace Robalo.Domain.Events;

/// <summary>
/// Occurrs when a user adds a message to a thread (so-called prompt)
/// </summary>
/// <param name="MessageNumber">Unique message number</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record UserMessageAdded(
    int MessageNumber,
    string Content,
    DateTimeOffset AddedOn) : MessageAdded(MessageNumber, Content, AddedOn)
{
    public static UserMessageAdded FromMessage(UserMessage message) => new(
        MessageNumber: message.Number,
        Content: message.Content,
        AddedOn: message.CreatedOn);
}