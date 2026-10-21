using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn => _messages.LastOrDefault()?.CreatedOn ?? CreatedOn;

    readonly List<Message> _messages = [];

    public IReadOnlyList<Message> Messages { get; }

    public string? Title { get; private set; }

    Option<int> _lastAnsweredMessage = None.Default;

    public bool IsAnswered { private set; get; }

    private int GetNextMessageNumber() => _messages.Count == 0 ? 1 : _messages[^1].Number + 1;

    Thread(ThreadIdentifier id, Version version, DateTimeOffset createdOn) : base(id, version)
    {
        CreatedOn = createdOn;
        Messages = _messages.AsReadOnly();

        When<UserMessageAdded>(@event =>
        {
            _messages.Add(Message.FromModel(@event));

            IsAnswered = false;
        });

        When<LlmMessageAdded>(@event =>
        {
            _messages.Add(Message.FromModel(@event));

            // Arriving LLM Message cannot change status to Unanswered
            if (IsAnswered)
            {
                return;
            }

            // Last answered message is max between the current LLM message and what's on record,
            // because it could be that it replies older messages, but newer have been replied already
            _lastAnsweredMessage = Math.Max(_lastAnsweredMessage.ValueOr(-1), @event.RepliedToMessage);

            if (_messages[_lastAnsweredMessage.ValueOrFailure..].All(m => m.Source == MessageSource.LLM))
            {
                IsAnswered = true;
            }
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

    public Thread AddLlmMessage(string content, int repliedToMessage, DateTimeOffset addedOn)
    {
        if (_messages.Count == 0)
        {
            throw new InvalidOperationException("Cannot add an LlmMessage to a thread without any user messages");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentOutOfRangeException.ThrowIfLessThan(repliedToMessage, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(repliedToMessage, _messages.Count);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);


        NewVersion = Version.NewVersion();
        return Apply(new LlmMessageAdded(GetNextMessageNumber(), content, repliedToMessage, addedOn));
    }

    public Thread AddUserMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        NewVersion = Version.NewVersion();
        return Apply(new UserMessageAdded(GetNextMessageNumber(), content, addedOn));
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
        IEnumerable<MessageAdded> events)
    {
        var thread = new Thread(identifier, version, createdOn) { Title = title };
        thread.ForEach(events, (t, e) => thread.Apply(e));

        thread.MarkAggregateAsSynchronized();

        return thread;
    }
}