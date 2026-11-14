using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Serilog;

namespace Robalo.Domain.Configuration;

public static class ConfigurationManagerExtensions
{
    public static ConfigurationManager AddRobaloConfiguration(this ConfigurationManager manager, int pollingIntervalSeconds = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pollingIntervalSeconds, 1);
        Extensions.AddDapperTypeHandlers();
        DatabaseOptions databaseOptions = new();
        manager.GetSection("Database").Bind(databaseOptions);

        var sqlConnectionFactory = new SqliteConnectionFactory(Log.Logger, databaseOptions.AsSettings());
        var repository = new SqliteConfigurationRepository(Log.Logger, sqlConnectionFactory);

        var configOrNone = repository.LoadConfiguration(CancellationToken.None).GetAwaiter().GetResult();
        if (configOrNone.IsNone)
        {
            throw new InvalidOperationException("No configuration found. Use init command to initialize Robalo application");
        }

        IConfigurationBuilder configBuilder = manager;
        configBuilder.Add(new ConfigurationRepositorySource(repository, TimeSpan.FromSeconds(pollingIntervalSeconds)));

        return manager;
    }
}