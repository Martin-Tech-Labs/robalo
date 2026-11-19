using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record Message(
    MessageIdentifier MessageId,
    Option<MessageIdentifier> OriginalMessageId,
    int Number,
    string EventType,
    string Content,
    Option<MessageIdentifier> RepliedToMessageId,
    DateTimeOffset CreatedOn)
{
    public static Message FromModel(MessageAdded messageAdded, int messageNumber)
    {
        return messageAdded switch
        {
            UserMessageAdded e => FromUserMessageAdded(e),
            UserMessageCopied e => FromUserMessageCopied(e),
            AssistantMessageAdded e => FromAssistantMessageAdded(e),
            AssistantMessageCopied e => FromAssistantMessageCopied(e),
            BreakMessageAdded e => FromBreakMessageAdded(e),
            SummaryMessageAdded e => FromSummaryMessageAdded(e),
            SummaryMessageCopied e => FromSummaryMessagCopied(e)
        };

        Message FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: None.Default,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }

        Message FromUserMessageCopied(UserMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }


        Message FromAssistantMessageAdded(AssistantMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: None.Default,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                CreatedOn: e.AddedOn);
        }

        Message FromAssistantMessageCopied(AssistantMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                CreatedOn: e.AddedOn);
        }

        Message FromBreakMessageAdded(BreakMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: None.Default,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }

        Message FromSummaryMessageAdded(SummaryMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: None.Default,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }

        Message FromSummaryMessagCopied(SummaryMessageCopied e)
        {
            return new(
                MessageId: e.MessageId,
                OriginalMessageId: e.OriginalMessageId,
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }
    }
}