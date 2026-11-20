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

    public EventDto() { }

    public EventDto(
        ThreadIdentifier threadId,
        MessageIdentifier messageId,
        MessageIdentifier? originalMessageId,
        string eventType,
        string content,
        MessageIdentifier? repliedToMessageId,
        string? referencedMessageIds,
        DateTimeOffset createdOn)
    {
        ThreadId = threadId;
        MessageId = messageId;
        OriginalMessageId = originalMessageId;
        EventType = eventType;
        Content = content;
        RepliedToMessageId = repliedToMessageId;
        CreatedOn = createdOn;
        ReferencedMessageIds = referencedMessageIds;
    }

    public static MessageAdded ToModel(EventDto dto)
    {
        return dto.EventType switch
        {
            nameof(UserMessageAdded) => ToUserMessageAdded(dto),
            nameof(AssistantMessageAdded) => ToAssistantMessageAdded(dto),
            nameof(SummaryMessageAdded) => ToSummaryMessageAdded(dto),
            nameof(BreakMessageAdded) => ToBreakMessageAdded(dto),
            nameof(UserMessageCopied) => ToUserMessageCopied(dto),
            nameof(AssistantMessageCopied) => ToAssistantMessageCopied(dto),
            nameof(SummaryMessageCopied) => ToSummaryMessageCopied(dto),

            _ => throw new ArgumentException($"Unexpected event type {dto.EventType}")
        };

        BreakMessageAdded ToBreakMessageAdded(EventDto dto)
        {
            return new BreakMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                AddedOn: dto.CreatedOn);
        }

        UserMessageAdded ToUserMessageAdded(EventDto dto)
        {
            return new UserMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                AddedOn: dto.CreatedOn);
        }

        UserMessageCopied ToUserMessageCopied(EventDto dto)
        {
            return new UserMessageCopied(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                OriginalMessageId: dto.OriginalMessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.OriginalMessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                AddedOn: dto.CreatedOn);
        }

        AssistantMessageAdded ToAssistantMessageAdded(EventDto dto)
        {
            return new AssistantMessageAdded(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                Content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
                RepliedToMessageId: dto.RepliedToMessageId!,
                AddedOn: dto.CreatedOn);
        }

        AssistantMessageCopied ToAssistantMessageCopied(EventDto dto)
        {
            return new AssistantMessageCopied(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                OriginalMessageId: dto.OriginalMessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.OriginalMessageId)),
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

        SummaryMessageCopied ToSummaryMessageCopied(EventDto dto)
        {
            return new SummaryMessageCopied(
                MessageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
                OriginalMessageId: dto.OriginalMessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.OriginalMessageId)),
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
           originalMessageId: null,
           eventType: e.GetType().Name,
           content: userMessageAdded.Content,
           repliedToMessageId: null,
           referencedMessageIds: null,
           createdOn: userMessageAdded.AddedOn),

        UserMessageCopied userMessageCopied => new EventDto(
            threadId: threadIdentifier,
            messageId: userMessageCopied.MessageId,
            originalMessageId: userMessageCopied.OriginalMessageId,
            eventType: e.GetType().Name,
            content: userMessageCopied.Content,
            repliedToMessageId: null,
            referencedMessageIds: null,
            createdOn: userMessageCopied.AddedOn),

        AssistantMessageAdded assistantMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: assistantMessageAdded.MessageId,
           originalMessageId: null,
           eventType: e.GetType().Name,
           content: assistantMessageAdded.Content,
           repliedToMessageId: assistantMessageAdded.RepliedToMessageId,
           referencedMessageIds: null,
           createdOn: assistantMessageAdded.AddedOn),

        AssistantMessageCopied assistantMessageCopied => new EventDto(
           threadId: threadIdentifier,
           messageId: assistantMessageCopied.MessageId,
           originalMessageId: assistantMessageCopied.OriginalMessageId,
           eventType: e.GetType().Name,
           content: assistantMessageCopied.Content,
           repliedToMessageId: assistantMessageCopied.RepliedToMessageId,
           referencedMessageIds: null,
           createdOn: assistantMessageCopied.AddedOn),

        BreakMessageAdded breakMessageAdded => new EventDto(
           threadId: threadIdentifier,
           messageId: breakMessageAdded.MessageId,
           originalMessageId: null,
           eventType: e.GetType().Name,
           content: breakMessageAdded.Content,
           repliedToMessageId: null,
           referencedMessageIds: null,
           createdOn: breakMessageAdded.AddedOn),

        SummaryMessageAdded summaryMessageAdded => new EventDto(
            threadId: threadIdentifier,
            messageId: summaryMessageAdded.MessageId,
            originalMessageId: null,
            eventType: e.GetType().Name,
            content: summaryMessageAdded.Content,
            repliedToMessageId: null,
            referencedMessageIds: string.Join(",", summaryMessageAdded.References),
            createdOn: summaryMessageAdded.AddedOn),

        SummaryMessageCopied summaryMessageCopied => new EventDto(
            threadId: threadIdentifier,
            messageId: summaryMessageCopied.MessageId,
            originalMessageId: summaryMessageCopied.OriginalMessageId,
            eventType: e.GetType().Name,
            content: summaryMessageCopied.Content,
            repliedToMessageId: null,
            referencedMessageIds: string.Join(",", summaryMessageCopied.References),
            createdOn: summaryMessageCopied.AddedOn)
    };
}