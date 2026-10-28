namespace Robalo.Domain.UnitTests.Models;

using Robalo.Domain.Models;
using SimpleBase;

public closed class BaseIdentifierTests<TIdentifier>(
    Func<TIdentifier> newIdentifier,
    Func<Guid, TIdentifier> fromUuid,
    Func<string, TIdentifier> fromIdString,
    string prefix) where TIdentifier : Identifier
{
    readonly Func<TIdentifier> _newIdentifier = newIdentifier;
    readonly Func<Guid, TIdentifier> _fromUuid = fromUuid;
    readonly Func<string, TIdentifier> _fromIdString = fromIdString;
    readonly string _prefix = prefix;

    private readonly Fixture _fixture = new();

    [Fact]
    public void NewThreadId_ShouldCreateCorrectId()
    {
        var id = _newIdentifier();

        id.ShouldNotBeNull();
        id.IdString.ShouldNotBeNullOrWhiteSpace();
        id.Uuid.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public void NewThreadId_ShouldProduceDifferentObjects()
    {
        var id1 = _newIdentifier();
        var id2 = _newIdentifier();

        id1.IdString.Equals(id2.IdString, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        id1.Uuid.ShouldNotBe(id2.Uuid);

        id1.Equals(id2).ShouldBeFalse();
        (id1 == id2).ShouldBeFalse();
        (id1 != id2).ShouldBeTrue();

        id1.GetHashCode().ShouldNotBe(id2.GetHashCode());
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldCorrectlyReconstructObject()
    {
        var id1 = _newIdentifier();
        var id2 = _fromUuid(id1.Uuid);

        id1.IdString.Equals(id2.IdString, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
        (id1 == id2).ShouldBeTrue();
        (id1 != id2).ShouldBeFalse();
    }

    [Fact]
    public void FromId_ShouldCorrectlyReconstructThreadId()
    {
        var id1 = _newIdentifier();
        var id2 = _fromIdString(id1.IdString);

        id1.IdString.Equals(id2.IdString, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        id1.Uuid.ShouldBe(id2.Uuid);
        id1.GetHashCode().ShouldBe(id2.GetHashCode());

        id1.Equals(id2).ShouldBeTrue();
        (id1 == id2).ShouldBeTrue();
        (id1 != id2).ShouldBeFalse();
    }


    [Fact]
    public void FromId_ShouldThrowOnInvalidPrefix()
    {
        var id1 = _newIdentifier();

        Should.Throw<ArgumentException>(() =>
        {
            _fromIdString(id1.IdString.Replace(_prefix, "abc_"));
        });
    }

    [Fact]
    public void FromId_ShouldThrowOnInvalidFormat()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _fromIdString(_fixture.Create<string>());
        });
    }


    [Fact]
    public void FromId_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _fromIdString($"{_prefix}{Base32.Rfc4648.Encode(Guid.Empty.ToByteArray(), false).ToLowerInvariant()}");
        });
    }

    [Fact]
    public void ThreadIdFromUuid_ShouldThrowOnEmptyUuid()
    {
        Should.Throw<ArgumentException>(() =>
        {
            _fromUuid(Guid.Empty);
        });
    }
}
