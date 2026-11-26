using Robalo.Domain.Models;

namespace Robalo.Domain.Services;

public interface ICompactThreadService
{
    Task CompactAndRefreshThread(Thread thread, CancellationToken cancellationToken);
}