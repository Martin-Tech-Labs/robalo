using Robalo.Domain.Events;

namespace Robalo.Domain;

public sealed class Thread : AggregateRoot
{
    public Version OldVersion { get; private set; }
    public Version NewVersion { get; private set; }
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn { get; private set; }
    public Identifier Id { get; }

    readonly List<Message> _messages = [];

    public string? Title { get; }

    Thread(Identifier id, Version version)
    {
        OldVersion = NewVersion = version;
        Id = id;

        When<UserMessageAdded>(@event =>
            _messages.Add(new Message(
                Id: @event.Id,
                Content: @event.Content,
                CreatedOn: @event.AddedOn))
        );
    }

    public static Thread NewThread(Identifier threadId)
    {
        if (threadId.Type != Identifier.IdentifierType.Thread)
        {
            throw new ArgumentOutOfRangeException(nameof(threadId), "Invalid id");
        }
        return new(threadId, Version.NewVersion());
    }

    public Thread AddUserMessage(Identifier messageId, string content, DateTimeOffset addedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        if (messageId.Type != Identifier.IdentifierType.Message)
        {
            throw new ArgumentException("Invalid id", nameof(messageId));
        }

        if (addedOn < ModifiedOn)
        {
            throw new ArgumentOutOfRangeException(nameof(addedOn), "Invalid date");
        }

        Apply(new UserMessageAdded(messageId, content, addedOn));
        return this;
    }
}