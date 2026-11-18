using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public record Message(
    MessageIdentifier MessageId,
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
            AssistantMessageAdded e => FromAssistantMessageAdded(e),
            BreakMessageAdded e => FromBreakMessageAdded(e)
        };

        Message FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
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
                Number: messageNumber,
                EventType: e.GetType().Name,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }
    }
}