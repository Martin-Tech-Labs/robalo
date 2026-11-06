namespace Robalo.Domain.Repositories.Sqlite.Dto;

public sealed record ConfigurationDto(
    int ConfigurationId,
    string Value,
    DateTimeOffset CreatedOn);