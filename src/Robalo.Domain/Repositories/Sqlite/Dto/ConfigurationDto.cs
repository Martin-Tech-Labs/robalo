namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record ConfigurationDto(
    int ConfigurationId,
    DateTimeOffset CreatedOn,
    string Value);