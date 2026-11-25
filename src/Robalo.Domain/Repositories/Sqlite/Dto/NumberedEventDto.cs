using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record NumberedEventDto : EventDto
{
    public int MessageNumber { get; set; }
    public NumberedEventDto() { }

    public NumberedEventDto(
        ThreadIdentifier threadId,
        MessageIdentifier messageId,
        MessageIdentifier? originalMessageId,
        int messageNumber,
        string eventType,
        string content,
        MessageIdentifier? repliedToMessageId,
        string? references,
        int? inputTokens,
        int? outputTokens,
        string? model,
        DateTimeOffset createdOn) : base(
            threadId: threadId,
            messageId: messageId,
            originalMessageId: originalMessageId,
            eventType: eventType,
            content: content,
            repliedToMessageId: repliedToMessageId,
            referencedMessageIds: references,
            promptTokens: inputTokens,
            tokens: outputTokens,
            model: model,
            createdOn: createdOn)
    {
        MessageNumber = messageNumber;
    }


    public static NumberedEventDto FromEventDto(EventDto dto, int messageNumber) => 
        new NumberedEventDto(
            threadId: dto.ThreadId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.ThreadId)),
            messageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
            originalMessageId: dto.OriginalMessageId,
            eventType: dto.EventType ?? throw new ArgumentNullException(nameof(dto), nameof(dto.EventType)),
            content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
            repliedToMessageId: dto.RepliedToMessageId,
            references: dto.ReferencedMessageIds,
            inputTokens: dto.PromptTokens,
            outputTokens: dto.Tokens,
            model: dto.Model,
            createdOn: dto.CreatedOn,
            messageNumber: messageNumber);
}