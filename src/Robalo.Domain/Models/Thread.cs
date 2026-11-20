using System.Runtime.Serialization;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models.Events;

namespace Robalo.Domain.Models;

public sealed class Thread : AggregateRoot<Thread>
{
    public DateTimeOffset CreatedOn { get; }
    public DateTimeOffset ModifiedOn { get; private set; }

    readonly List<ContextMessage> _context = [];
    readonly List<ContextMessage> _extendedContext = [];

    /// <summary>
    /// Current context of a thread, logically ordered
    /// </summary>
    public IReadOnlyList<ContextMessage> Context { get; }

    /// <summary>
    /// Temporary extended context of a thread (limited to one previous segment as separated by breaks)
    /// To copy messages over, or create a summary
    /// </summary>
    public IReadOnlyList<ContextMessage> ExtendedContext { get; }

    public string? Title { get; private set; }

    Option<MessageIdentifier> _lastUserMessageId = None.Default;
    public bool IsAnswered { private set; get; }

    public bool LastUserCopy { private set; get; }

    Thread(ThreadIdentifier id, Option<int> persistedVersion, DateTimeOffset createdOn) : base(id, persistedVersion)
    {
        CreatedOn = ModifiedOn = createdOn;
        Context = _context.AsReadOnly();
        ExtendedContext = _extendedContext.AsReadOnly();
        IsAnswered = true;

        OnSaved += () =>
        {
            _extendedContext.Clear();
        };

        When<BreakMessageAdded>(@event =>
        {
            _extendedContext.Clear();
            _extendedContext.AddRange(_context);
            _context.Clear();
            IsAnswered = true;
            LastUserCopy = false;
            ModifiedOn = @event.AddedOn;
            _lastUserMessageId = None.Default;
        });

        When<UserMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            IsAnswered = false;
            LastUserCopy = false;
            ModifiedOn = @event.AddedOn;

            _lastUserMessageId = message.MessageId;
        });

        When<SummaryMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;

            IsAnswered = IsAnswered || LastUserCopy;
            LastUserCopy = false;
        });

        When<AssistantMessageCopied>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;


            IsAnswered = IsAnswered || LastUserCopy;
            LastUserCopy = false;
        });

        When<UserMessageCopied>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;

            if (IsAnswered == true)
            {
                IsAnswered = false;
                LastUserCopy = true;
            }
        });

        When<SummaryMessageCopied>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;

            IsAnswered = IsAnswered || LastUserCopy;
            LastUserCopy = false;
        });

        When<AssistantMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            ModifiedOn = @event.AddedOn;

            var repliedToMessageIndex = _context.FindIndex(m => m.MessageId == @event.RepliedToMessageId);

            // Should not really happen (repliedToMessage could be in the extended context), but if so, we still need to rehydrate the aggregate
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
        if (_context.Count == 0 && _extendedContext.Count == 0)
        {
            throw new InvalidOperationException("Cannot create an assistant message with an empty context");
        }

        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        var messageIndex = _context.FindIndex(m => m.MessageId == repliedToMessage);
        if (messageIndex == -1)
        {
            messageIndex = _extendedContext.FindIndex(m => m.MessageId == repliedToMessage);
        }

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

    public Thread CopyMessage(ContextMessage message, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);

        if (_extendedContext.Count == 0)
        {
            throw new InvalidOperationException("Copying of a message to a new segment after a break is only possible before the thread has been persisted.");
        }

        if (!_extendedContext.Contains(message))
        {
            throw new ArgumentException("Invalid message", nameof(message));
        }

        if (_context.Any(c => c.OriginalMessageId == message.OriginalMessageId))
        {
            throw new ArgumentException("Duplicate message", nameof(message));
        }

        MessageAdded ev = message.Role switch
        {
            MessageRole.User => new UserMessageCopied(
                MessageId: MessageIdentifier.NewIdentifier(),
                OriginalMessageId: message.OriginalMessageId,
                Content: message.Content,
                AddedOn: addedOn),

            MessageRole.Assistant => new AssistantMessageCopied(
                MessageId: MessageIdentifier.NewIdentifier(),
                OriginalMessageId: message.OriginalMessageId,
                Content: message.Content,
                RepliedToMessageId: message.RepliedToMessageId.ValueOrFailure,
                AddedOn: addedOn),

            MessageRole.Summary => new SummaryMessageCopied(
                MessageId: MessageIdentifier.NewIdentifier(),
                OriginalMessageId: message.OriginalMessageId,
                Content: message.Content,
                References: message.References.ValueOrFailure,
                AddedOn: addedOn),

            _ => throw new InvalidOperationException($"Role {message.Role} not supported")
        };

        return Apply(ev);
    }

    public Thread AddSummaryMessage(string content, List<MessageIdentifier> messageIds, DateTimeOffset addedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentOutOfRangeException.ThrowIfLessThan(messageIds.Count, 1);

        if (_extendedContext.Count == 0)
        {
            throw new InvalidOperationException("Copying of a message to a new segment after a break is only possible before the thread has been persisted.");
        }

        if (messageIds.Any(m => !_extendedContext.Any(e => e.MessageId == m)))
        {
            throw new ArgumentException("Invalid message ids", nameof(messageIds));
        }

        return Apply(new SummaryMessageAdded(MessageIdentifier.NewIdentifier(), content, messageIds, addedOn));
    }

    public Thread AddBreakMessage(string content, DateTimeOffset addedOn)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentException.ThrowIfNullOrWhiteSpace(content);

        if (_context.Count == 0)
        {
            throw new InvalidOperationException("Cannot create a break with an empty context");
        }

        if (_extendedContext.Count != 0)
        {
            throw new InvalidOperationException("Cannot create a break with a pending break change. Persist the aggregate and try again.");
        }

        if (PersistedVersion.IsNone)
        {
            throw new InvalidOperationException("Cannot create a break for a newly created and not-saved thread. Persist the aggregate and try again.");
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