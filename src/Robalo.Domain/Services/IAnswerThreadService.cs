using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Services;

public interface IAnswerThreadService
{
    Task<SuccessOrFailure> Answer(ThreadIdentifier threadId, CancellationToken cancellationToken);
}