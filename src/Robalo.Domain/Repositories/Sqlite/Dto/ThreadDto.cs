using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record ThreadDto(
    string Id,
    string? Title,
    DateTimeOffset CreatedOn,
    DateTimeOffset ModifiedOn,
    int Version)
{
    public static ThreadDto FromModel(Thread thread) => new(
        Id: thread.Id.IdString,
        Title: thread.Title,
        CreatedOn: thread.CreatedOn,
        ModifiedOn: thread.ModifiedOn,
        Version: thread.PendingVersion);

    public ThreadInfo ToModel() => new(ThreadIdentifier.FromIdString(Id), Title, CreatedOn, ModifiedOn, Version);

    public static ThreadInfo ToModel(ThreadDto dto) => dto.ToModel();
}