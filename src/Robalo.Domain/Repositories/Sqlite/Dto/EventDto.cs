using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record EventDto(
    ThreadIdentifier ThreadId,
    int MessageNumber,
    string EventType,
    string Content,
    int? RepliedToMessage,
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
                MessageNumber: dto.MessageNumber,
                Content: dto.Content,
                AddedOn: dto.CreatedOn);
        }

        LlmMessageAdded ToLlmMessageAdded(EventDto dto)
        {
            return new LlmMessageAdded(
                MessageNumber: dto.MessageNumber,
                Content: dto.Content,
                RepliedToMessage: dto.RepliedToMessage!.Value,
                AddedOn: dto.CreatedOn);
        }
    }

    public static EventDto FromModel(ThreadIdentifier threadIdentifier, MessageAdded e) => e switch
    {
        UserMessageAdded userMessageAdded => new EventDto(
           ThreadId: threadIdentifier,
           MessageNumber: userMessageAdded.MessageNumber,
           EventType: e.GetType().Name,
           Content: userMessageAdded.Content,
           RepliedToMessage: e is LlmMessageAdded llmMessageAdded1 ? llmMessageAdded1.RepliedToMessage : null,
           CreatedOn: userMessageAdded.AddedOn),

        LlmMessageAdded llmMessageAdded => new EventDto(
           ThreadId: threadIdentifier,
           MessageNumber: llmMessageAdded.MessageNumber,
           EventType: e.GetType().Name,
           Content: llmMessageAdded.Content,
           RepliedToMessage: llmMessageAdded.RepliedToMessage,
           CreatedOn: llmMessageAdded.AddedOn)
    };

    public static Message ToMessage(EventDto dto)
    {
        var messageSource = dto.EventType switch
        {
            nameof(UserMessageAdded) => MessageSource.User,
            nameof(LlmMessageAdded) => MessageSource.LLM,

            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };
        return new(
            Number: dto.MessageNumber,
            Source: messageSource,
            Content: dto.Content,
            RepliedToMessage: dto.RepliedToMessage.AsOption(),
            CreatedOn: dto.CreatedOn);
    }
}