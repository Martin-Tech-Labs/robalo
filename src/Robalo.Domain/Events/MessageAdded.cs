using Robalo.Common.Extensions;
using Robalo.Domain.Models;

namespace Robalo.Domain.Events;

/// <summary>
/// Occurrs when a user adds a message to a thread (so-called prompt)
/// </summary>
/// <param name="MessageNumber">Unique message number</param>
/// <param name="Content">Content of the message</param>
/// <param name="AddedOn">Timestamp of the message</param>
public closed record MessageAdded(
    MessageIdentifier MessageId,
    string Content,
    DateTimeOffset AddedOn);