using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record Message(
    MessageIdentifier MessageId,
    int Number,
    MessageSource Source,
    string Content,
    Option<MessageIdentifier> RepliedToMessageId,
    DateTimeOffset CreatedOn)
{
    public static Message FromModel(MessageAdded messageAdded, int messageNumber)
    {
        return messageAdded switch
        {
            UserMessageAdded e => FromUserMessageAdded(e),
            LlmMessageAdded e => FromLlmMessageAdded(e)
        };

        Message FromUserMessageAdded(UserMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Number: messageNumber,
                Source: MessageSource.User,
                Content: e.Content,
                RepliedToMessageId: None.Default,
                CreatedOn: e.AddedOn);
        }

        Message FromLlmMessageAdded(LlmMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Number: messageNumber,
                Source: MessageSource.LLM,
                Content: e.Content,
                RepliedToMessageId: e.RepliedToMessageId.AsOption(),
                CreatedOn: e.AddedOn);
        }
    }
}