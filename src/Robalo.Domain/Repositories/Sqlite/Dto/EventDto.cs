using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public record EventDto
{
    public ThreadIdentifier? ThreadId { set; get; }
    public MessageIdentifier? MessageId { set; get; }
    public string? EventType { set; get; }
    public string? Content { set; get; }
    public MessageIdentifier? RepliedToMessageId { set; get; }
    public DateTimeOffset CreatedOn { set; get; }

    public EventDto()
    {

    }

    public EventDto(
        ThreadIdentifier threadId,
        MessageIdentifier messageId,
        string eventType,
        string content,
        MessageIdentifier? repliedToMessageId,
        DateTimeOffset createdOn)
    {
        ThreadId = threadId;
        MessageId = messageId;
        EventType = eventType;
        Content = content;
        RepliedToMessageId = repliedToMessageId;
        CreatedOn = createdOn;
    }

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
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                AddedOn: dto.CreatedOn);
        }

        LlmMessageAdded ToLlmMessageAdded(EventDto dto)
        {
            return new LlmMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                RepliedToMessageId: dto.RepliedToMessageId!,
                AddedOn: dto.CreatedOn);
        }
    }

    public static EventDto FromModel(ThreadIdentifier threadIdentifier, MessageAdded e) => e switch
    {
        UserMessageAdded userMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: userMessageAdded.MessageId,
           eventType: e.GetType().Name,
           content: userMessageAdded.Content,
           repliedToMessageId: e is LlmMessageAdded llmMessageAdded1 ? llmMessageAdded1.RepliedToMessageId : null,
           createdOn: userMessageAdded.AddedOn),

        LlmMessageAdded llmMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: llmMessageAdded.MessageId,
           eventType: e.GetType().Name,
           content: llmMessageAdded.Content,
           repliedToMessageId: llmMessageAdded.RepliedToMessageId,
           createdOn: llmMessageAdded.AddedOn)
    };
}