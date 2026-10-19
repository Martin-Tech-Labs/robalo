using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.UnitTests.Repositories;

public class InMemoryThreadRepositoryTests : BaseThreadRepositoryTests<InMemoryThreadRepository>
{
    public InMemoryThreadRepositoryTests(): base(new InMemoryThreadRepository(Log.Logger))
    {
    }
}