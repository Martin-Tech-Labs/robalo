using Robalo.Common.Extensions;
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
            _messages.Add(new Message(
                Number: ++_lastMessageNumber,
                Source: MessageSource.User,
                Content: @event.Content,
                CreatedOn: @event.AddedOn));
        });
    }

    public static Thread NewThread(DateTimeOffset createdOn)
    {
        if (createdOn == default)
        {
            throw new ArgumentOutOfRangeException(nameof(createdOn), "Invalid date");
        }

        return new(ThreadIdentifier.NewIdentifier(), Version.NewVersion(), createdOn);
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

    public Thread AddUserMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        NewVersion = Version.NewVersion();
        return Apply(new UserMessageAdded(content, addedOn));
    }

    public Thread UpdateTitle(string title)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        Title = title;

        NewVersion = Version.NewVersion();
        return this;
    }

    public static Thread Rehydrate(
        ThreadIdentifier identifier,
        DateTimeOffset createdOn,
        string? title,
        Version version,
        IEnumerable<UserMessageAdded> events)
    {
        var thread = new Thread(identifier, version, createdOn) { Title = title };
        thread.ForEach(events, (t, e) => thread.Apply(e));

        thread.MarkAggregateAsSynchronized();

        return thread;
    }
}