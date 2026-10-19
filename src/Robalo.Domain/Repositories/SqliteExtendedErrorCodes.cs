namespace Robalo.Domain.Repositories;

#pragma warning disable CA1707
public static class SqliteExtendedErrorCodes
{
    /// <summary>
    ///  Extended error code for SQLITE_CONSTRAINT indicating that a PRIMARY KEY constraint failed.
    /// </summary>
    public const int SQLITE_CONSTRAINT_PRIMARYKEY = 1555;
}