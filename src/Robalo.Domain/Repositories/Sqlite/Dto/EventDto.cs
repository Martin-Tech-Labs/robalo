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
    public string? ReferencedMessageIds { set; get; }
    public MessageIdentifier? OriginalMessageId { set; get; }
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
        string? references,

        DateTimeOffset createdOn)
    {
        ThreadId = threadId;
        MessageId = messageId;
        EventType = eventType;
        Content = content;
        RepliedToMessageId = repliedToMessageId;
        CreatedOn = createdOn;
        ReferencedMessageIds = references;
    }

    public static MessageAdded ToModel(EventDto dto)
    {
        return dto.EventType switch
        {
            nameof(UserMessageAdded) => ToUserMessageAdded(dto),
            nameof(AssistantMessageAdded) => ToLlmMessageAdded(dto),
            nameof(SummaryMessageAdded) => ToSummaryMessageAdded(dto),

            _ => throw new ArgumentOutOfRangeException(nameof(dto))
        };

        UserMessageAdded ToUserMessageAdded(EventDto dto)
        {
            return new UserMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                AddedOn: dto.CreatedOn);
        }

        AssistantMessageAdded ToLlmMessageAdded(EventDto dto)
        {
            return new AssistantMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                RepliedToMessageId: dto.RepliedToMessageId!,
                AddedOn: dto.CreatedOn);
        }

        SummaryMessageAdded ToSummaryMessageAdded(EventDto dto)
        {
            return new SummaryMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                References: dto.ReferencedMessageIds!.Split(',').Select(MessageIdentifier.FromIdString).ToList(),
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
           repliedToMessageId: null,
           references: null,
           createdOn: userMessageAdded.AddedOn),

        AssistantMessageAdded llmMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: llmMessageAdded.MessageId,
           eventType: e.GetType().Name,
           content: llmMessageAdded.Content,
           repliedToMessageId: llmMessageAdded.RepliedToMessageId,
           references: null,
           createdOn: llmMessageAdded.AddedOn),

        BreakMessageAdded breakMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: breakMessageAdded.MessageId,
           eventType: e.GetType().Name,
           content: breakMessageAdded.Content,
           repliedToMessageId: null,
           references: null,
           createdOn: breakMessageAdded.AddedOn),

        SummaryMessageAdded summaryMessageAdded => new EventDto(
            threadId: threadIdentifier,
            messageId: summaryMessageAdded.MessageId,
            eventType: e.GetType().Name,
            content: summaryMessageAdded.Content,
            repliedToMessageId: null,
            references: string.Join(",", summaryMessageAdded.References),
            createdOn: summaryMessageAdded.AddedOn),

            _ => throw new InvalidOperationException("tbd")
    };
}