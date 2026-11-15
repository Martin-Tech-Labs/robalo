using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Robalo.Common.Models;
using Robalo.Domain.Repositories.Sqlite;
using Robalo.Domain.Repositories.Sqlite.Configuration;
using Serilog;

namespace Robalo.Domain.Configuration;

public static class ConfigurationManagerExtensions
{
    public static ConfigurationManager AddRobaloConfiguration(
        this ConfigurationManager manager,
        IServiceCollection services,
        Option<string> filePathOrNone,
        int pollingIntervalSeconds = 5)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pollingIntervalSeconds, 1);
        Extensions.AddDapperTypeHandlers();

        var databaseSettins = filePathOrNone switch
        {
            Some<string> filePath => DatabaseSettings.Create(filePath),
            _ => DatabaseSettings.Create()
        };

        var sqlConnectionFactory = new SqliteConnectionFactory(Log.Logger, databaseSettins);
        var repository = new SqliteConfigurationRepository(Log.Logger, sqlConnectionFactory);

        var configOrNone = repository.LoadConfiguration(CancellationToken.None).GetAwaiter().GetResult();
        if (configOrNone.IsNone)
        {
            throw new InvalidOperationException("No configuration found. Use init command to initialize Robalo application");
        }

        IConfigurationBuilder configBuilder = manager;
        configBuilder.Add(new ConfigurationRepositorySource(repository, TimeSpan.FromSeconds(pollingIntervalSeconds)));

        services.AddSingleton<ISqliteConnectionFactory>(sqlConnectionFactory);
        return manager;
    }
}