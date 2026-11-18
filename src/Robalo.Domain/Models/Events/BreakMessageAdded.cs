using Robalo.Common.Extensions;
using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when a break is added to a thread to separate segments
/// </summary>
/// <param name="MessageId">Unique message id</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public record BreakMessageAdded(
    MessageIdentifier MessageId,
    string Content,
    DateTimeOffset AddedOn) : MessageAdded(MessageId, Content, AddedOn);