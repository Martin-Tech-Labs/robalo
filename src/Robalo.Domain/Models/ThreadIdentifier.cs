using Newtonsoft.Json;
using Robalo.Common.Models;

namespace Robalo.Domain.Models;

public sealed class ThreadIdentifier : Identifier, IEquatable<ThreadIdentifier>
{
    protected override string Prefix => "trd_";

    private ThreadIdentifier(string idString) : base(idString) { }

    private ThreadIdentifier(Guid uuid) : base(uuid) { }

    public static implicit operator string(ThreadIdentifier value) => value.ToString();

    public static Option<ThreadIdentifier> TryGetFromId(string id)
    {
        try
        {
            return FromIdString(id);
        }
        catch (ArgumentException)
        {
            return None.Default;
        }
    }

    public static ThreadIdentifier FromIdString(string id) => new(id);

    public static ThreadIdentifier FromUuid(Guid uuid) => new(uuid);

    public static ThreadIdentifier NewIdentifier() => FromUuid(Guid.NewGuid());

    public bool Equals(ThreadIdentifier? other) => base.Equals(other);

    public override bool Equals(object? obj) => base.Equals(obj);

    public override int GetHashCode() => base.GetHashCode();

    public static bool operator ==(ThreadIdentifier id1, ThreadIdentifier id2) => id1.Equals(id2);
    public static bool operator !=(ThreadIdentifier id1, ThreadIdentifier id2) => !id1.Equals(id2);
}