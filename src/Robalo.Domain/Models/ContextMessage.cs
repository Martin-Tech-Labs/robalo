using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record ContextMessage(
    MessageIdentifier MessageId,
    MessageSource Source,
    string Content,
    Option<MessageIdentifier> RepliedToMessageId)
{
    public static ContextMessage FromModel(MessageAdded messageAdded)
    {
        return messageAdded switch
        {
            UserMessageAdded e => FromUserMessageAdded(e),
            LlmMessageAdded e => FromLlmMessageAdded(e)
        };

        ContextMessage FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Source: MessageSource.User,
                Content: e.Content,
                RepliedToMessageId: None.Default);
        }

        ContextMessage FromLlmMessageAdded(LlmMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Source: MessageSource.LLM,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption());
        }
    }
}