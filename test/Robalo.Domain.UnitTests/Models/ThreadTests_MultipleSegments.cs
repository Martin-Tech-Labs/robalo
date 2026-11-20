using System.Reflection.Metadata;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Domain.Models;
using Robalo.Domain.Models.Events;
using Robalo.Tests.Common;

namespace Robalo.Domain.UnitTests.Models;

public class ThreadTests_MultipleSegments
{
    readonly Fixture _fixture = new();
    readonly DateTimeOffset _utcNow;

    public ThreadTests_MultipleSegments()
    {
        _utcNow = _fixture.Create<DateTimeOffset>();
    }

}