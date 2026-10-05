using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn => _messages.LastOrDefault()?.CreatedOn ?? CreatedOn;

    readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    public string? Title { get; private set; }

    Thread(Identifier id, Version version, DateTimeOffset createdOn) : base(id, version)
    {
        CreatedOn = createdOn;
        When<UserMessageAdded>(@event =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@event.Content);
            if (@event.Id.Type != Identifier.IdentifierType.Message)
            {
                throw new ArgumentException("Invalid id", nameof(@event));
            }

            if (@event.AddedOn < ModifiedOn)
            {
                throw new ArgumentException("Invalid date", nameof(@event));
            }

            _messages.Add(new Message(
                Id: @event.Id,
                Source: MessageSource.User,
                Content: @event.Content,
                CreatedOn: @event.AddedOn));
        });
    }

    public static Thread NewThread(Identifier threadId, DateTimeOffset createdOn)
    {
        if (threadId.Type != Identifier.IdentifierType.Thread)
        {
            throw new ArgumentOutOfRangeException(nameof(threadId), "Invalid id");
        }

        if (createdOn == default)
        {
            throw new ArgumentOutOfRangeException(nameof(createdOn), "Invalid date");
        }

        return new(threadId, Version.NewVersion(), createdOn);
    }

    public Thread AddUserMessage(Identifier messageId, string content, DateTimeOffset addedOn)
    {
        Apply(new UserMessageAdded(messageId, content, addedOn));

        NewVersion = Version.NewVersion();
        return this;
    }

    public Thread UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;

        NewVersion = Version.NewVersion();
        return this;
    }
}