using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public record Message(
    MessageIdentifier MessageId,
    int Number,
    MessageSource Source,
    string Content,
    Option<int> RepliedToMessage,
    DateTimeOffset CreatedOn)
{
    public static Message FromModel(MessageAdded messageAdded)
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
                Number: e.MessageNumber,
                Source: MessageSource.User,
                Content: e.Content,
                RepliedToMessage: None.Default,
                CreatedOn: e.AddedOn);
        }

        Message FromLlmMessageAdded(LlmMessageAdded e)
        {
            return new(
                MessageId: e.MessageId,
                Number: e.MessageNumber,
                Source: MessageSource.LLM,
                Content: e.Content,
                RepliedToMessage: e.RepliedToMessage.AsOption(),
                CreatedOn: e.AddedOn);
        }
    }
}