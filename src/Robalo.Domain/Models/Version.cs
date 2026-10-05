namespace Robalo.Domain.Models;

public sealed class Version : IEquatable<Version>
{
    public string VersionString { get; }

    Version(Guid versionUuid) => VersionString = versionUuid.ToString("N").ToLowerInvariant();

    public static Version NewVersion() => new(Guid.NewGuid());
    public static Version FromVersionString(string versionString)
    {
        if (Guid.TryParseExact(versionString, "N", out var guid))
        {
            if (guid == Guid.Empty)
            {
                throw new ArgumentException("Wrong format");
            }
            return new(guid);
        }

        throw new ArgumentException("Wrong format");
    }

    public override string ToString() => VersionString;

    public override bool Equals(object? obj) => obj is Version other && other.Equals(this);

    public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(VersionString);

    public bool Equals(Version? other) => other?.VersionString == VersionString;
}