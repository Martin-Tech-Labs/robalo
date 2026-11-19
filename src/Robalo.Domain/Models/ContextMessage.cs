using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record ContextMessage(
    MessageIdentifier MessageId,
    MessageIdentifier OriginalMessageId,
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
            UserMessageCopied e => FromUserMessageCopied(e),
            AssistantMessageAdded e => FromAssistantMessageAdded(e),
            AssistantMessageCopied e => FromAssistantMessageCopied(e),
            SummaryMessageAdded e => FromSummaryMessageAdded(e),
            SummaryMessageCopied e => FromSummaryMessageCopied(e),
            BreakMessageAdded => throw new ArgumentException($"Unsupported type {messageAdded.GetType().Name}")
        };

        ContextMessage FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.MessageId,
                Role: MessageRole.User,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: None.Default);
        }

        ContextMessage FromUserMessageCopied(UserMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Role: MessageRole.User,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: None.Default);
        }

        ContextMessage FromSummaryMessageAdded(SummaryMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.MessageId,
                Role: MessageRole.Summary,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: e.References.Some());
        }

        ContextMessage FromSummaryMessageCopied(SummaryMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Role: MessageRole.Summary,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                References: e.References.Some());
        }

        ContextMessage FromAssistantMessageAdded(AssistantMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.MessageId,
                Role: MessageRole.Assistant,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                References: None.Default);
        }

        ContextMessage FromAssistantMessageCopied(AssistantMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Role: MessageRole.Assistant,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                References: None.Default);
        }
    }
}