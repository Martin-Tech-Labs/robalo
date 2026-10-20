using Robalo.Common.Extensions;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn => _messages.LastOrDefault()?.CreatedOn ?? CreatedOn;

    readonly List<UserMessage> _messages = [];

    public IReadOnlyList<UserMessage> Messages { get; }

    public string? Title { get; private set; }

    private int GetNextMessageNumber() => _messages.Count == 0 ? 1 : _messages[^1].Number + 1;

    Thread(ThreadIdentifier id, Version version, DateTimeOffset createdOn) : base(id, version)
    {
        CreatedOn = createdOn;
        Messages = _messages.AsReadOnly();

        When<UserMessageAdded>(@event =>
        {
            _messages.Add(new UserMessage(
                Number: @event.MessageNumber,
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

    public Thread AddLlmMessage(string content, DateTimeOffset addedOn, int firstMessageNumber, int lastMessageNumber)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(lastMessageNumber, firstMessageNumber);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(lastMessageNumber, _messages[^1].Number);

        NewVersion = Version.NewVersion();
        return Apply(new LlmMessageAdded(
            MessageNumber: GetNextMessageNumber(),
            Content: content,
            AddedOn: addedOn,
            FirstMessageNumber: firstMessageNumber,
            LastMessageNumber: lastMessageNumber));
    }

    public Thread AddUserMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        NewVersion = Version.NewVersion();
        return Apply(new UserMessageAdded(
            MessageNumber: GetNextMessageNumber(),
            Content: content,
            AddedOn: addedOn));
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