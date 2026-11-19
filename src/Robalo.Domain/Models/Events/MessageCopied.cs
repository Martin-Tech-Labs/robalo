using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Models.Events;

/// <summary>
/// Occurrs when a messages gets copied
/// </summary>
/// <param name="MessageId">Unique message id</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public closed record MessageCopied(
    MessageIdentifier MessageId,
    MessageIdentifier OriginalMessageId,
    string Content,
    DateTimeOffset AddedOn) : MessageAdded(MessageId, Content, AddedOn);