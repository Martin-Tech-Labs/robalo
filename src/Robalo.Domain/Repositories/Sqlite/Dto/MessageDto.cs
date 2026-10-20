using Robalo.Domain.Events;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record MessageDto(
    ThreadIdentifier ThreadId,
    long MessageNumber,
    string Source,
    string Content,
    DateTimeOffset CreatedOn)
{
    public static MessageDto FromModel(ThreadIdentifier threadIdentifier, UserMessage message) =>
         new(
            ThreadId: threadIdentifier,
            MessageNumber: message.Number,
            Source: MessageSource.User.ToString().ToLowerInvariant(),
            Content: message.Content,
            CreatedOn: message.CreatedOn);

    public static MessageDto FromEvent(ThreadIdentifier threadIdentifier, UserMessageAdded e) =>
        new(
            ThreadId: threadIdentifier,
            MessageNumber: e.MessageNumber,
            Source: MessageSource.User.ToString().ToLowerInvariant(),
            Content: e.Content,
            CreatedOn: e.AddedOn);

    public UserMessage ToModel() =>
        new((int)MessageNumber, Content, CreatedOn);

    public static UserMessage ToModel(MessageDto dto) => dto.ToModel();
}