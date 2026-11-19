using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when a user adds a message to a thread (so-called prompt)
/// </summary>
/// <param name="MessageId">Unique message id</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record UserMessageCopied(
    MessageIdentifier MessageId,
    MessageIdentifier OriginalMessageId,
    string Content,
    DateTimeOffset AddedOn) : MessageCopied(MessageId, OriginalMessageId, Content, AddedOn);