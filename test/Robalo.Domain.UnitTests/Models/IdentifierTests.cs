namespace Robalo.Domain.UnitTests.Models;

using Robalo.Domain.Models;
using SimpleBase;

public class IdentifierTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void NewThreadId_ShouldCreateCorrectId()
    {
        var id = Identifier.NewThreadId();

        id.ShouldNotBeNull();
        id.Id.ShouldNotBeNullOrWhiteSpace();
        id.Uuid.ShouldNotBe(Guid.Empty);
        id.Type.ShouldBe(Identifier.IdentifierType.Thread);
    }

    [Fact]
    public void NewThreadId_ShouldProduceDifferentObjects()
    {
        var id1 = Identifier.NewThreadId();
        var id2 = Identifier.NewThreadId();

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeFalse();
        id1.Uuid.ShouldNotBe(id2.Uuid);

        id1.Equals(id2).ShouldBeFalse();

        id1.GetHashCode().ShouldNotBe(id2.GetHashCode());
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldCorrectlyReconstructObject()
    {
        var id1 = Identifier.NewThreadId();
        var id2 = Identifier.ThreadIdFromUuid(id1.Uuid);

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.Type.ShouldBe(id2.Type);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void NewMessageId_ShouldCreateCorrectId()
    {
        var id = Identifier.NewMessageId();

        id.ShouldNotBeNull();
        id.Id.ShouldNotBeNullOrWhiteSpace();
        id.Uuid.ShouldNotBe(Guid.Empty);
        id.Type.ShouldBe(Identifier.IdentifierType.Message);
    }

    [Fact]
    public void NewMessageId_ShouldProduceDifferentObjects()
    {
        var id1 = Identifier.NewMessageId();
        var id2 = Identifier.NewMessageId();

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeFalse();
        id1.Uuid.ShouldNotBe(id2.Uuid);

        id1.Equals(id2).ShouldBeFalse();

        id1.GetHashCode().ShouldNotBe(id2.GetHashCode());
    }

    [Fact]
    public void MessageIdFromUuid_ShouldCorrectlyReconstructObject()
    {
        var id1 = Identifier.NewMessageId();
        var id2 = Identifier.MessageIdFromUuid(id1.Uuid);

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.Type.ShouldBe(id2.Type);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void FromId_ShouldCorrectlyReconstructThreadId()
    {
        var id1 = Identifier.NewThreadId();
        var id2 = Identifier.FromId(id1.Id);

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.Type.ShouldBe(id2.Type);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void FromId_ShouldCorrectlyReconstructMessageId()
    {
        var id1 = Identifier.NewMessageId();
        var id2 = Identifier.FromId(id1.Id);

        id1.Id.Equals(id2.Id, StringComparison.InvariantCultureIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.Type.ShouldBe(id2.Type);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
    }

    [Fact]
    public void FromId_ShouldThrowOnInvalidPrefix()
    {
        var id1 = Identifier.NewThreadId();

        Should.Throw<ArgumentException>(() =>
        {
            Identifier.FromId(id1.Id.Replace("trd", "abc"));
        });
    }

    [Fact]
    public void FromId_ShouldThrowOnInvalidFormat()
    {
        Should.Throw<ArgumentException>(() =>
        {
            Identifier.FromId(_fixture.Create<string>());
        });
    }


    [Fact]
    public void FromId_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            Identifier.FromId($"trd_{Base32.Rfc4648.Encode(Guid.Empty.ToByteArray(), false).ToLowerInvariant()}");
        });
    }

    [Fact]
    public void MessageIdFromUuid_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            Identifier.MessageIdFromUuid(Guid.Empty);
        });
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            Identifier.ThreadIdFromUuid(Guid.Empty);
        });
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldReturnCorrectType()
    {
        Identifier.ThreadIdFromUuid(_fixture.Create<Guid>()).Type.ShouldBe(Identifier.IdentifierType.Thread);
    }

    [Fact]
    public void MessageIdFromUuid_ShouldReturnCorrectType()
    {
        Identifier.MessageIdFromUuid(_fixture.Create<Guid>()).Type.ShouldBe(Identifier.IdentifierType.Message);
    }

    [Fact]
    public void FromUuid_ShouldReturnDifferentIdsForSameUUIDButDifferentType()
    {
        var uuid = Guid.NewGuid();
        var id1 = Identifier.ThreadIdFromUuid(uuid);
        var id2 = Identifier.MessageIdFromUuid(uuid);

        id1.Uuid.ShouldBe(id2.Uuid);
        id1.Type.ShouldNotBe(id2.Type);
        id1.Id.ShouldNotBe(id2.Id);

        id1.Equals(id2).ShouldBeFalse();
        id1.GetHashCode().ShouldNotBe(id2.GetHashCode());
    }


}
