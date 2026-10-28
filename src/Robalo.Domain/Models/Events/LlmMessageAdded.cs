using Robalo.Common.Extensions;
using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when a user adds a message to a thread (so-called prompt)
/// </summary>
/// <param name="MessageNumber">Unique message number</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record LlmMessageAdded(
    MessageIdentifier MessageId,
    string Content,
    MessageIdentifier RepliedToMessageId,
    DateTimeOffset AddedOn) : MessageAdded(MessageId, Content, AddedOn);