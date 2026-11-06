namespace Robalo.Domain.Models.MessageBus;

public record ThreadUpdated(ThreadIdentifier ThreadId)
{
    public static ThreadUpdated Create(ThreadIdentifier threadId) => new(threadId);

    public override string ToString() => $"{nameof(ThreadUpdated)}:{ThreadId}";
}