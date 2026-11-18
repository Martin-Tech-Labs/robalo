using Robalo.Common.Extensions;
using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when an assistant (llm) adds a message to a thread (so-called completion)
/// </summary>
/// <param name="MessageId">Unique message id</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record AssistantMessageAdded(
    MessageIdentifier MessageId,
    string Content,
    MessageIdentifier RepliedToMessageId,
    DateTimeOffset AddedOn) : MessageAdded(MessageId, Content, AddedOn);