using Robalo.Domain.Models;

namespace Robalo.Domain.Repositories.Dto;

public sealed record ThreadDto(
    string Id,
    string? Title,
    DateTimeOffset CreatedOn,
    DateTimeOffset ModifiedOn,
    string Version)
{
    public static ThreadDto FromModel(Thread thread) => new(
        Id: thread.Id.IdString,
        Title: thread.Title,
        CreatedOn: thread.CreatedOn,
        ModifiedOn: thread.ModifiedOn,
        Version: thread.NewVersion.VersionString);

    public ThreadInfo ToModel() => new(ThreadIdentifier.FromIdString(Id), Title, CreatedOn, ModifiedOn);

    public static ThreadInfo ToModel(ThreadDto dto) => dto.ToModel();
}