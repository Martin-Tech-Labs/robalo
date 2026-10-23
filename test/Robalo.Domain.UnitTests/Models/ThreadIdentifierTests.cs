using Robalo.Domain.Models;

namespace Robalo.Domain.UnitTests.Models;


public class ThreadIdentifierTests : BaseIdentifierTests<ThreadIdentifier>
{
    public ThreadIdentifierTests() : base(
        () => ThreadIdentifier.NewIdentifier(),
        ThreadIdentifier.FromUuid,
        ThreadIdentifier.FromIdString, 
        "trd_")
    {
    }
}