using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record EventDto(
    ThreadIdentifier ThreadId,
    MessageIdentifier MessageId,
    int MessageNumber,
    string EventType,
    string Content,
    MessageIdentifier? RepliedToMessageId,
    DateTimeOffset CreatedOn)
{
    public static MessageAdded ToModel(EventDto dto)
    {
        return dto.EventType switch
        {
            nameof(UserMessageAdded) => ToUserMessageAdded(dto),
            nameof(LlmMessageAdded) => ToLlmMessageAdded(dto),

            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

        UserMessageAdded ToUserMessageAdded(EventDto dto)
        {
            return new UserMessageAdded(
                MessageId: dto.MessageId,
                MessageNumber: dto.MessageNumber,
                Content: dto.Content,
                AddedOn: dto.CreatedOn);
        }

        LlmMessageAdded ToLlmMessageAdded(EventDto dto)
        {
            return new LlmMessageAdded(
                MessageId: dto.MessageId,
                MessageNumber: dto.MessageNumber,
                Content: dto.Content,
                RepliedToMessageId: dto.RepliedToMessageId!,
                AddedOn: dto.CreatedOn);
        }
    }

    public static EventDto FromModel(ThreadIdentifier threadIdentifier, MessageAdded e) => e switch
    {
        UserMessageAdded userMessageAdded => new EventDto(
           ThreadId: threadIdentifier,
           MessageId: userMessageAdded.MessageId,
           MessageNumber: userMessageAdded.MessageNumber,
           EventType: e.GetType().Name,
           Content: userMessageAdded.Content,
           RepliedToMessageId: e is LlmMessageAdded llmMessageAdded1 ? llmMessageAdded1.RepliedToMessageId : null,
           CreatedOn: userMessageAdded.AddedOn),

        LlmMessageAdded llmMessageAdded => new EventDto(
           ThreadId: threadIdentifier,
           MessageId: llmMessageAdded.MessageId,
           MessageNumber: llmMessageAdded.MessageNumber,
           EventType: e.GetType().Name,
           Content: llmMessageAdded.Content,
           RepliedToMessageId: llmMessageAdded.RepliedToMessageId,
           CreatedOn: llmMessageAdded.AddedOn)
    };
}