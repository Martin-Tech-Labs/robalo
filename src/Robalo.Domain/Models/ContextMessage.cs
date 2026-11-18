using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record ContextMessage(
    MessageIdentifier MessageId,
    MessageRole Source,
    string Content,
    Option<MessageIdentifier> RepliedToMessageId)
{
    public static ContextMessage FromModel(MessageAdded messageAdded)
    {
        return messageAdded switch
        {
            UserMessageAdded e => FromUserMessageAdded(e),
            AssistantMessageAdded e => FromLlmMessageAdded(e),

            _ => throw new ArgumentException($"Wrong type {messageAdded.GetType().Name}")
        };

        ContextMessage FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Source: MessageRole.User,
                Content: e.Content,
                RepliedToMessageId: None.Default);
        }

        ContextMessage FromLlmMessageAdded(AssistantMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Source: MessageRole.Assistant,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption());
        }
    }
}