using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn { get; private set; }

    readonly List<ContextMessage> _context = [];

    public IReadOnlyList<ContextMessage> Context { get; }

    public string? Title { get; private set; }

    Option<MessageIdentifier> _lastUserMessageId = None.Default;
    public bool IsAnswered { private set; get; }

    Thread(ThreadIdentifier id, Option<int> persistedVersion, DateTimeOffset createdOn) : base(id, persistedVersion)
    {
        CreatedOn = ModifiedOn = createdOn;
        Context = _context.AsReadOnly();
        IsAnswered = true;

        When<BreakMessageAdded>(@event =>
        {
            _context.Clear();
            IsAnswered = true;
            ModifiedOn = @event.AddedOn;
            _lastUserMessageId = None.Default;
        });

        When<UserMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            IsAnswered = false;
            ModifiedOn = @event.AddedOn;

            _lastUserMessageId = message.MessageId;
        });

        When<AssistantMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            ModifiedOn = @event.AddedOn;

            var repliedToMessageIndex = _context.FindIndex(m => m.MessageId == @event.RepliedToMessageId);

            // Should not really happen, but if so, we still need to rehydrate the aggregate
            if (repliedToMessageIndex == -1)
            {
                _context.Add(message);
                return;
            }

            IsAnswered = IsAnswered ||  // LLM reply cannot mark thread Unanswered
                message.RepliedToMessageId == _lastUserMessageId ||// LLM replied directly to the last user message 
                repliedToMessageIndex > _context.FindIndex(m => m.MessageId == _lastUserMessageId); // LLM replied to the message past user message

            _context.Insert(1 + Math.Max(
                repliedToMessageIndex,
                _context.FindLastIndex(m => m.RepliedToMessageId == @event.RepliedToMessageId)),
                message);
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

    public AssistantMessageAdded CreateAssistantMessageAdded(string content, MessageIdentifier repliedToMessage, DateTimeOffset addedOn)
    {
        if (_context.Count == 0)
        {
            throw new InvalidOperationException("Cannot create an assistant message with an empty context");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        var messageIndex = _context.FindIndex(m => m.MessageId == repliedToMessage);
        if (messageIndex == -1)
        {
            throw new ArgumentException("Invalid message", nameof(repliedToMessage));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        return new AssistantMessageAdded(MessageIdentifier.NewIdentifier(), content, repliedToMessage, addedOn);
    }

    public Thread AddAssistantMessage(string content, MessageIdentifier repliedToMessage, DateTimeOffset addedOn) =>
        Apply(CreateAssistantMessageAdded(content, repliedToMessage, addedOn));

    public static UserMessageAdded CreateUserMessageAdded(string content, DateTimeOffset addedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        return new UserMessageAdded(MessageIdentifier.NewIdentifier(), content, addedOn);
    }

    public Thread AddUserMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);

        return Apply(CreateUserMessageAdded(content, addedOn));
    }

    public Thread AddBreakMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        if (_context.Count == 0)
        {
            throw new InvalidOperationException("Cannot create a break with an empty context");
        }
        return Apply(new BreakMessageAdded(MessageIdentifier.NewIdentifier(), content, addedOn));
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

        var thread = new Thread(identifier, persistedVersion - events.Count, createdOn) { Title = title };
        thread.ForEach(events, (t, e) => t.Apply(e));

        if (events.Count > 0)
        {
            thread.MarkAggregateAsSynchronized();
        }

        return thread;
    }
}