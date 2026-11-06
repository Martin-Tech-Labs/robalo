using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Robalo.Common.Models;
using Robalo.Domain.Repositories;

namespace Robalo.Domain.Configuration;

public sealed class ConfigurationRepositoryProvider : ConfigurationProvider
{
    readonly IConfigurationRepository _repository;
    readonly ILogger _logger;

    public ConfigurationRepositoryProvider(ILogger logger, IConfigurationRepository repository)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;
    }

    public override void Load()
    {
        var configurationOrNone = _repository.LoadConfiguration(CancellationToken.None).GetAwaiter().GetResult();
        if (configurationOrNone is Some<RobaloConfiguration> configuration)
        {
            try
            {
                configuration.Value.Value.Verify();
                var json = JsonSerializer.Serialize(configuration.Value.Value);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                var parsed = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                Data = parsed.AsEnumerable()
                    .ToDictionary(x => $"Robalo:{x.Key}", x => x.Value, StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception exc)
            {
                _logger.Error(exc, "Configuration {ConfigurationId} is not valid and cannot be loaded", configuration.Value.ConfigurationId);
                throw;
            }
        }
        else
        {
            _logger.Error("Could not load configuration from the repository: No configuration found");
#pragma warning disable CA2201 // Do not raise reserved exception types
            throw new Exception("No configuration found");
#pragma warning restore CA2201 // Do not raise reserved exception types
        }
    }
}