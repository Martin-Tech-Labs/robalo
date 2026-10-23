using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record NumberedEventDto : EventDto
{
    public int MessageNumber { get; set; }
    public NumberedEventDto() { }

    public NumberedEventDto(
        ThreadIdentifier threadId,
        MessageIdentifier messageId,
        int messageNumber,
        string eventType,
        string content,
        MessageIdentifier? repliedToMessageId,
        DateTimeOffset createdOn) : base(threadId, messageId, eventType, content, repliedToMessageId, createdOn)
    {
        MessageNumber = messageNumber;
    }


    public static NumberedEventDto FromEventDto(EventDto dto, int messageNumber)
    {
        return new NumberedEventDto(
            threadId: dto.ThreadId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.ThreadId)),
            messageId: dto.MessageId ?? throw new ArgumentNullException(nameof(dto), nameof(dto.MessageId)),
            eventType: dto.EventType ?? throw new ArgumentNullException(nameof(dto), nameof(dto.EventType)),
            content: dto.Content ?? throw new ArgumentNullException(nameof(dto), nameof(dto.Content)),
            repliedToMessageId: dto.RepliedToMessageId,
            createdOn: dto.CreatedOn,
            messageNumber: messageNumber);
    }
}