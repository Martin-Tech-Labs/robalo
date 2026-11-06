using Microsoft.Extensions.Configuration;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Serilog;

namespace Robalo.Domain.Configuration;

public static class ConfigurationManagerExtensions
{
    public static ConfigurationManager AddRobaloConfiguration(this ConfigurationManager manager)
    {
        Extensions.AddDapperTypeHandlers();
        DatabaseOptions databaseOptions = new();
        manager.GetSection("Database").Bind(databaseOptions);

        var sqlConnectionFactory = new SqliteConnectionFactory(Log.Logger, databaseOptions.AsSettings());
        var repository = new SqliteConfigurationRepository(Log.Logger, sqlConnectionFactory);

        IConfigurationBuilder configBuilder = manager;
        configBuilder.Add(new ConfigurationRepositorySource(repository));

        return manager;
    }
}