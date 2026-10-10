using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn => _messages.LastOrDefault()?.CreatedOn ?? CreatedOn;

    readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages { get; }

    public string? Title { get; private set; }

    int _lastMessageNumber;

    Thread(ThreadIdentifier id, Version version, DateTimeOffset createdOn) : base(id, version)
    {
        CreatedOn = createdOn;
        Messages = _messages.AsReadOnly();
        
        When<UserMessageAdded>(@event =>
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(@event.Content);

            if (@event.AddedOn < ModifiedOn)
            {
                throw new ArgumentException("Invalid date", nameof(@event));
            }

            NewVersion = Version.NewVersion();

            _messages.Add(new Message(
                Number: ++_lastMessageNumber,
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

    public Thread SetLastMessageNumber(int lastMessageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(lastMessageNumber, 0);

        if (_messages.Count != 0 || _lastMessageNumber != 0 || OldVersion.HasValue)
        {
            throw new InvalidOperationException("Not supported");
        }

        _lastMessageNumber = lastMessageNumber;

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