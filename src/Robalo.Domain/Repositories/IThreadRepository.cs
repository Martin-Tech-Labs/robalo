using Robalo.Common.Models;
using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories;

public interface IThreadRepository
{
    Task<Option<Thread>> GetThread(Identifier identifier, CancellationToken cancellationToken);
    Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken);
    Task<SuccessOrNotFound> DeleteThread(Identifier identifier, CancellationToken cancellationToken);
    Task<Option<IAsyncEnumerable<Message>>> GetMessages(Identifier identifier, CancellationToken cancellationToken);
}