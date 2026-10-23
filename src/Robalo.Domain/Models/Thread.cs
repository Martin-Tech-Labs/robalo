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

    Option<int> _lastAnsweredMessageIndex = None.Default;

    public bool IsAnswered { private set; get; }

    private int GetNextMessageNumber() => PersistedVersion.ValueOr(0) + PendingEvents.Count + 1;

    Thread(ThreadIdentifier id, Option<int> persistedVersion, DateTimeOffset createdOn) : base(id, persistedVersion)
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

            var repliedToMessageIndex = _messages.FindIndex(m => m.MessageId == @event.RepliedToMessageId);

            // Should not really happen, but if so, we still need to rehydrate the aggregate
            if (repliedToMessageIndex == -1)
            {
                return;
            }

            // Last answered message is max between the current LLM message and what's on record,
            // because it could be that it replies older messages, but newer have been replied already
            _lastAnsweredMessageIndex = Math.Max(_lastAnsweredMessageIndex.ValueOr(-1), repliedToMessageIndex);

            if (_messages[(_lastAnsweredMessageIndex.ValueOrFailure + 1)..].All(m => m.Source == MessageSource.LLM))
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

        return new(ThreadIdentifier.NewIdentifier(), None.Default, createdOn);
    }

    public Thread AddLlmMessage(string content, Message repliedToMessage, DateTimeOffset addedOn)
    {
        if (_messages.Count == 0)
        {
            throw new InvalidOperationException("Cannot add an LlmMessage to a thread without any user messages");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        var messageIndex = _messages.FindIndex(m => ReferenceEquals(m, repliedToMessage));
        if (messageIndex == -1)
        {
            throw new ArgumentException("Invalid message", nameof(repliedToMessage));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return Apply(new LlmMessageAdded(
            MessageIdentifier.NewIdentifier(),
            GetNextMessageNumber(),
            content,
            repliedToMessage.MessageId,
            addedOn));
    }

    public Thread AddUserMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return Apply(new UserMessageAdded(MessageIdentifier.NewIdentifier(), GetNextMessageNumber(), content, addedOn));
    }

    public static Thread Rehydrate(
        ThreadIdentifier identifier,
        DateTimeOffset createdOn,
        string? title,
        int persistedVersion,
        IReadOnlyList<MessageAdded> events)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(persistedVersion, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(persistedVersion, events.Count);

        var thread = new Thread(identifier, persistedVersion, createdOn) { Title = title };
        thread.ReplayEvents(events);

        return thread;
    }
}