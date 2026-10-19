using Robalo.Domain.Events;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Dto;

// ThreadId TEXT,
// MessageNumber INTEGER,
// Source TEXT,
// Content TEXT,
// CreatedOn TEXT,

public sealed record MessageDto(
    string ThreadId,
    long MessageNumber,
    string Source,
    string Content,
    DateTimeOffset CreatedOn)
{
    public static MessageDto FromModel(ThreadIdentifier threadIdentifier, Message message) =>
         new(
            threadIdentifier.IdString,
            message.Number,
            message.Source.ToString().ToLowerInvariant(),
            message.Content,
            message.CreatedOn);

    public Message ToModel() =>
        new((int)MessageNumber, MessageSource.User, Content, CreatedOn);

    public static Message ToModel(MessageDto dto) => dto.ToModel();
}