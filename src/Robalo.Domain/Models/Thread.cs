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

        void OnUserMessage(MessageAdded @event)
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            IsAnswered = false;
            ModifiedOn = @event.AddedOn;

            _lastUserMessageId = message.MessageId;
        }

        void OnAssistantMessage(MessageAdded @event)
        {
            ModifiedOn = @event.AddedOn;

            var repliedToMessageId = @event switch
            {
                AssistantMessageAdded e => e.RepliedToMessageId,
                AssistantMessageCopied e => e.RepliedToMessageId,
                _ => throw new InvalidOperationException($"Unexpected type {@event.GetType().Name}")
            };

            var repliedToMessageIndex = _context.FindIndex(m => m.MessageId == repliedToMessageId);

            // Should not happen that often (repliedToMessage could be in the extended context and not copied over), but if so, we still need to rehydrate the aggregate
            if (repliedToMessageIndex == -1)
            {
                // We should not add PromptTokens if this is a "detached" message
                _context.Add(ContextMessage.FromModel(@event) with { PromptTokens = None.Default });
                return;
            }

            var message = ContextMessage.FromModel(@event);

            IsAnswered = IsAnswered ||  // LLM reply cannot mark thread Unanswered
                message.RepliedToMessageId == _lastUserMessageId ||// LLM replied directly to the last user message 
                repliedToMessageIndex > _context.FindIndex(m => m.MessageId == _lastUserMessageId); // LLM replied to the message past user message

            // For AssistantMessageAdded we want always make sure, that the assistant message is added right after the user message,
            // or after its replies (if there are multiple assistant messages for the same user message)
            // This is especially relevant, when a new user message arrives before the assistant messages gets added,
            // as we may end up in this order: UserMessage1, UserMessage2, AssistantMessage1(for UserMessage1), but what we want is this:
            // UserMessage1, AssistantMessage1, UserMessage2, so that the subsequent prompt to the LLM uses this logical order

            // It also covers this case: U1,U2,A1(U1),A2(U1),A3(U2) -> U1, A1(U1), A2(U1), U2, A3(U2) 

            if (@event is AssistantMessageAdded)
            {
                _context.Insert(1 + Math.Max(
                    repliedToMessageIndex,
                    _context.FindLastIndex(m => m.RepliedToMessageId == repliedToMessageId)),
                    message);
            }
            // We do not do logic re-ordering for copy assistant messages, as they are added during the compaction (break) phase,
            // with no concurrent user messages interfering in the middle.
            // Though, it should not happen, we leave it up to the compaction service to change the ordering if it wishes so.
            else
            {
                _context.Add(message);
            }
        }

        When<BreakMessageAdded>(@event =>
        {
            _extendedContext.Clear();
            _extendedContext.AddRange(_context);
            _context.Clear();
            IsAnswered = true;
            ModifiedOn = @event.AddedOn;
            _lastUserMessageId = None.Default;
        });

        When<UserMessageAdded>(OnUserMessage);
        When<UserMessageCopied>(OnUserMessage);

        When<SummaryMessageAdded>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;
        });

        When<SummaryMessageCopied>(@event =>
        {
            var message = ContextMessage.FromModel(@event);
            _context.Add(message);
            ModifiedOn = @event.AddedOn;
        });

        When<AssistantMessageCopied>(OnAssistantMessage);
        When<AssistantMessageAdded>(OnAssistantMessage);
    }

    public static Thread NewThread(DateTimeOffset createdOn)
    {
        if (createdOn == default)
        {
            throw new ArgumentOutOfRangeException(nameof(createdOn), "Invalid date");
        }

        return new(ThreadIdentifier.NewIdentifier(), None.Default, createdOn);
    }

    public AssistantMessageAdded CreateAssistantMessageAdded(
        string content,
        MessageIdentifier repliedToMessage,
        Option<int> promptTokensOrNone,
        Option<int> tokensOrNone,
        string model,
        DateTimeOffset addedOn)
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
        ArgumentException.ThrowIfNullOrWhiteSpace(model);

        if (tokensOrNone is Some<int> tokens)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(tokens.Value, 1, nameof(tokensOrNone));
        }

        if (promptTokensOrNone is Some<int> promptTokens)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(promptTokens.Value, 1, nameof(promptTokensOrNone));
        }

        return new AssistantMessageAdded(
            MessageId: MessageIdentifier.NewIdentifier(),
            Content: content,
            RepliedToMessageId: repliedToMessage,
            Tokens: tokensOrNone,
            PromptTokens: promptTokensOrNone,
            Model: model,
            AddedOn: addedOn);
    }

    public Thread AddAssistantMessage(
        string content,
        MessageIdentifier repliedToMessage,
        Option<int> promptTokens,
        Option<int> tokens,
        string model,
        DateTimeOffset addedOn) =>
        Apply(CreateAssistantMessageAdded(
            content: content,
            repliedToMessage: repliedToMessage,
            promptTokensOrNone: promptTokens,
            tokensOrNone: tokens,
            model: model,
            addedOn: addedOn));

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

    public Thread AddCopyOfMessage(ContextMessage message, DateTimeOffset addedOn)
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
                // Try resolve RepliedToMessageId against copied message using OriginalId
                RepliedToMessageId: ResolveRepliedToMessageId(message.RepliedToMessageId.ValueOrFailure),
                Tokens: message.Tokens,
                Model: message.Model.ValueOrFailure,
                AddedOn: addedOn),

            MessageRole.Summary => new SummaryMessageCopied(
                MessageId: MessageIdentifier.NewIdentifier(),
                OriginalMessageId: message.OriginalMessageId,
                Content: message.Content,
                References: message.References.ValueOrFailure,
                Tokens: message.Tokens,
                Model: message.Model.ValueOrFailure,
                AddedOn: addedOn),

            _ => throw new InvalidOperationException($"Role {message.Role} not supported")
        };

        MessageIdentifier ResolveRepliedToMessageId(MessageIdentifier repliedToMessageId)
        {
            var originalUserMessageId = _extendedContext.SingleOrDefault(c => c.MessageId == repliedToMessageId)?.OriginalMessageId;

            if (originalUserMessageId is null)
            {
                return repliedToMessageId;
            }

            return _context.SingleOrDefault(c => c.OriginalMessageId == originalUserMessageId)?.MessageId ?? repliedToMessageId;
        }

        return Apply(ev);
    }

    public Thread AddSummaryMessage(string content, IReadOnlyList<MessageIdentifier> messageIds, Option<int> tokensOrNone, string model, DateTimeOffset addedOn)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(model);
        if (tokensOrNone is Some<int> tokens)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(tokens.Value, 1, nameof(tokensOrNone));
        }
        ArgumentOutOfRangeException.ThrowIfLessThan(addedOn, ModifiedOn);
        ArgumentOutOfRangeException.ThrowIfLessThan(messageIds.Count, 1);

        if (_extendedContext.Count == 0)
        {
            throw new InvalidOperationException("Copying of a message to a new segment after a break is only possible before the thread has been persisted.");
        }

        if (messageIds.Distinct().Count() != messageIds.Count)
        {
            throw new ArgumentException("Duplicate message ids", nameof(messageIds));
        }

        if (messageIds.Any(m => !_extendedContext.Any(e => e.MessageId == m)))
        {
            throw new ArgumentException("Invalid message ids", nameof(messageIds));
        }

        return Apply(new SummaryMessageAdded(MessageIdentifier.NewIdentifier(), content, messageIds, tokensOrNone, model, addedOn));
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