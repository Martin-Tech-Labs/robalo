using Robalo.Common.Extensions;
using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when an assistant (llm) message gets copied
/// </summary>
/// <param name="MessageId">Unique message id</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record AssistantMessageCopied(
    MessageIdentifier MessageId,
    MessageIdentifier OriginalMessageId,
    string Content,
    MessageIdentifier RepliedToMessageId,
    DateTimeOffset AddedOn) : MessageCopied(MessageId, OriginalMessageId, Content, AddedOn);