namespace Robalo.Domain.UnitTests.Models;

using Robalo.Domain.Models;

public class VersionTests
{
    private readonly Fixture _fixture = new();

    [Fact]
    public void NewVersion_ShouldCreateNewVersion()
    {
        var version = Version.NewVersion();

        version.ShouldNotBeNull();
        version.VersionString.ShouldNotBeNull();
    }

    [Fact]
    public void NewVersion_ShouldProduceDifferentObjects()
    {
        var version1 = Version.NewVersion();
        var version2 = Version.NewVersion();

        version1.VersionString.Equals(version2.VersionString, StringComparison.OrdinalIgnoreCase).ShouldBeFalse();
        version1.Equals(version2).ShouldBeFalse();
        version1.GetHashCode().ShouldNotBe(version2.GetHashCode());
    }

    [Fact]
    public void FromVersionString_ShouldCorrectlyReconstructVersion()
    {
        var version1 = Version.NewVersion();
        var version2 = Version.FromVersionString(version1.VersionString);

        version1.VersionString.Equals(version2.VersionString, StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
        version1.Equals(version2).ShouldBeTrue();
        version1.GetHashCode().ShouldBe(version2.GetHashCode());
    }

    [Fact]
    public void FromVersionString_ShouldThrowOnInvalidString()
    {
        Should.Throw<ArgumentException>(() => Version.FromVersionString(_fixture.Create<string>()));
    }

    [Fact]
    public void FromVersionString_ShouldThrowOnEmptyGuid()
    {
        Should.Throw<ArgumentException>(() => Version.FromVersionString(Guid.Empty.ToString("N")));
    }
}
