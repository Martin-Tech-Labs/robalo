namespace Robalo.Domain.UnitTests.Models;

using Robalo.Domain.Models;
using SimpleBase;

public class ThreadIdentifierTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void NewThreadId_ShouldCreateCorrectId()
    {
        var id = ThreadIdentifier.NewIdentifier();

        id.ShouldNotBeNull();
        id.Id.ShouldNotBeNullOrWhiteSpace();
        id.Uuid.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewThreadId_ShouldProduceDifferentObjects()
    {
        var id1 = ThreadIdentifier.NewIdentifier();
        var id2 = ThreadIdentifier.NewIdentifier();

        id1.Id.Equals(id2.Id, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        id1.Uuid.ShouldNotBe(id2.Uuid);

        id1.Equals(id2).ShouldBeFalse();

        id1.GetHashCode().ShouldNotBe(id2.GetHashCode());
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldCorrectlyReconstructObject()
    {
        var id1 = ThreadIdentifier.NewIdentifier();
        var id2 = ThreadIdentifier.FromUuid(id1.Uuid);

        id1.Id.Equals(id2.Id, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void FromId_ShouldCorrectlyReconstructThreadId()
    {
        var id1 = ThreadIdentifier.NewIdentifier();
        var id2 = ThreadIdentifier.FromId(id1.Id);

        id1.Id.Equals(id2.Id, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }


    [Fact]
    public void FromId_ShouldThrowOnInvalidPrefix()
    {
        var id1 = ThreadIdentifier.NewIdentifier();

        Should.Throw<ArgumentException>(() =>
        {
            ThreadIdentifier.FromId(id1.Id.Replace("trd", "abc"));
        });
    }

    [Fact]
    public void FromId_ShouldThrowOnInvalidFormat()
    {
        Should.Throw<ArgumentException>(() =>
        {
            ThreadIdentifier.FromId(_fixture.Create<string>());
        });
    }


    [Fact]
    public void FromId_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            ThreadIdentifier.FromId($"trd_{Base32.Rfc4648.Encode(Guid.Empty.ToByteArray(), false).ToLowerInvariant()}");
        });
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            ThreadIdentifier.FromUuid(Guid.Empty);
        });
    }
}
