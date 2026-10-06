using Robalo.Common.Models;
using Robalo.Domain.Models;
using Thread = Robalo.Domain.Models.Thread;

namespace Robalo.Domain.Repositories;

public interface IThreadRepository
{
    Task<Option<Thread>> GetThread(Identifier identifier, CancellationToken cancellationToken);
    Task<UpdateResult> SaveThread(Thread thread, CancellationToken cancellationToken);
    Task<SuccessOrNotFound> DeleteThread(Identifier identifier, CancellationToken cancellationToken);
    Task<Option<IAsyncEnumerable<Message>>> GetMessages(Thread thread, CancellationToken cancellationToken);
}