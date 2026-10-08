using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories;

public interface IThreadRepository
{
    Task<Option<Thread>> GetThread(ThreadIdentifier identifier, CancellationToken cancellationToken);
    Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken);
    Task<SuccessOrNotFound> DeleteThread(ThreadIdentifier identifier, CancellationToken cancellationToken);
    Task<Option<IAsyncEnumerable<Message>>> GetMessages(ThreadIdentifier identifier, CancellationToken cancellationToken);
    Task<Option<Message>> GetMessage(ThreadIdentifier identifier, int messageNumber, CancellationToken cancellationToken);
}