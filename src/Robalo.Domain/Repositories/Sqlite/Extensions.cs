using Dapper;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Robalo.Domain.Repositories.Sqlite.Configuration;

namespace Robalo.Domain.Repositories.Sqlite;

public static class Extensions
{
    public static IServiceCollection AddSqliteRepositories(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.AddTransient<IThreadRepository, SqliteThreadRepository>();
        services.AddTransient<IConfigurationRepository, SqliteConfigurationRepository>();

        return services;
    }

    public static void AddDapperTypeHandlers()
    {
        SqlMapper.RemoveTypeMap(typeof(DateTimeOffset));
        SqlMapper.AddTypeHandler(DateTimeOffsetTypeHandler.Default);
        SqlMapper.RemoveTypeMap(typeof(int));
        SqlMapper.AddTypeHandler(IntTypeHandler.Default);
        SqlMapper.AddTypeHandler(VersionTypeHandler.Default);
        SqlMapper.AddTypeHandler(ThreadIdentifierTypeHandler.Default);
        SqlMapper.AddTypeHandler(MessageIdentifierTypeHandler.Default);
    }
}