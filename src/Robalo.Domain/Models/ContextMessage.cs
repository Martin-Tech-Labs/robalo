using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record ContextMessage(
    MessageIdentifier MessageId,
    MessageRole Role,
    string Content,
    Option<MessageIdentifier> RepliedToMessageId,
    Option<IReadOnlyList<MessageIdentifier>> References)
{
    public static ContextMessage FromModel(MessageAdded messageAdded)
    {
        return messageAdded switch
        {
            UserMessageAdded e => FromUserMessageAdded(e),
            AssistantMessageAdded e => FromLlmMessageAdded(e),
            SummaryMessageAdded e => FromSummaryMessageAdded(e),

            _ => throw new ArgumentException($"Wrong type {messageAdded.GetType().Name}")
        };

        ContextMessage FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Role: MessageRole.User,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: None.Default);
        }

        ContextMessage FromSummaryMessageAdded(SummaryMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Role: MessageRole.Summary,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: e.References.Some());
        }

        ContextMessage FromLlmMessageAdded(AssistantMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Role: MessageRole.Assistant,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                References: None.Default);
        }
    }
}