using Robalo.Domain.Models;

namespace Robalo.Domain.Services;

public interface IAnswerThreadService
{
    Task Answer(ThreadIdentifier threadIdentifier, CancellationToken cancellationToken);
}