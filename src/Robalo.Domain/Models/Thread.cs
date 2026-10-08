using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn => _messages.LastOrDefault()?.CreatedOn ?? CreatedOn;

    readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages => _messages.AsReadOnly();

    public string? Title { get; private set; }

    public long LastMessageNumber { get; private set; }

    Thread(ThreadIdentifier id, Version version, DateTimeOffset createdOn) : base(id, version)
    {
        CreatedOn = createdOn;
        When<UserMessageAdded>(@event =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@event.Content);

            if (@event.AddedOn < ModifiedOn)
            {
                throw new ArgumentException("Invalid date", nameof(@event));
            }

            NewVersion = Version.NewVersion();

            _messages.Add(new Message(
                Number: ++LastMessageNumber,
                Source: MessageSource.User,
                Content: @event.Content,
                CreatedOn: @event.AddedOn));
        });
    }

    public static Thread NewThread(ThreadIdentifier threadId, DateTimeOffset createdOn)
    {
        if (createdOn == default)
        {
            throw new ArgumentOutOfRangeException(nameof(createdOn), "Invalid date");
        }

        return new(threadId, Version.NewVersion(), createdOn);
    }

    public Thread SetLastMessageNumber(long lastMessageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lastMessageNumber, 0);

        if (_messages.Count != 0)
        {
            throw new InvalidOperationException("Not supported");
        }

        if (LastMessageNumber != 0)
        {
            throw new InvalidOperationException("Not supported");
        }

        if (OldVersion.HasValue)
        {
            throw new InvalidOperationException("Not supported");
        }

        LastMessageNumber = lastMessageNumber;

        return this;
    }

    public Thread AddUserMessage(string content, DateTimeOffset addedOn) =>
        Apply(new UserMessageAdded(content, addedOn));

    public Thread UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;

        NewVersion = Version.NewVersion();
        return this;
    }
}