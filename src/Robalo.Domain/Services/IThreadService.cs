using Robalo.Domain.Models;

namespace Robalo.Domain.Services;

public interface IThreadService
{
    public Task ProcessUnansweredMessages(ThreadIdentifier threadIdentifier, CancellationToken cancellationToken);
}