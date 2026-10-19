using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Robalo.Domain.Repositories.Sqlite.Configuration;

namespace Robalo.Domain.Repositories.Sqlite;

public static class Extensions
{
    public static IServiceCollection AddSqliteRepository(
        this IServiceCollection services,
        ConfigurationManager configuration,
        string configurationKey = "Database")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(configurationKey);

        AddDapperTypeHandlers();

        services.AddTransient<IThreadRepository, SqliteThreadRepository>();
        services.AddTransient<ISqliteConnectionFactory, SqliteConnectionFactory>();

        DatabaseOptions databaseOptions = new();

        configuration.GetSection(configurationKey).Bind(databaseOptions);

        var databaseSettings = databaseOptions.AsSettings();
        services.AddSingleton(databaseSettings);

        return services;
    }

    public static void AddDapperTypeHandlers()
    {
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.AddTypeHandler(DateTimeOffsetTypeHandler.Default);
        SqlMapper.AddTypeHandler(VersionTypeHandler.Default);
        SqlMapper.AddTypeHandler(ThreadIdentifierTypeHandler.Default);
    }
}