using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Robalo.Common.Extensions;
using Robalo.Common.Models;
using Robalo.Common.Rop;
using Robalo.Common.Semaphore;
using Robalo.Domain.Repositories;

namespace Robalo.Domain.Configuration;

public sealed class ConfigurationRepositoryProvider : ConfigurationProvider, IDisposable, IAsyncDisposable
{
    readonly IConfigurationRepository _repository;
    Option<int> _currentConfigurationId = None.Default;
    readonly ILogger _logger;
    readonly Timer _timer;
    readonly Lock _lock = new();

    public ConfigurationRepositoryProvider(ILogger logger, IConfigurationRepository repository, TimeSpan pollingInterval)
    {
        _logger = logger.ForContext(GetType());
        _repository = repository;

        _timer = new Timer(
            callback: _ => Load(),
            state: null,
            dueTime: pollingInterval,
            period: pollingInterval);
    }

    public override void Load()
    {
        lock (_lock)
        {
            _Load();
        }
    }

    void _Load()
    {
        var _ = LoadConfiguration()
            .Bind(CheckConfiguration)
            .Map(LoadConfigurationToData);

        Result<Option<RobaloConfiguration>, None> LoadConfiguration()
        {
            try
            {
                return _repository.LoadConfiguration(CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception exc)
            {
                _logger.Error(exc, "Failed loading configuration from the repository");
                if (_currentConfigurationId.IsNone)
                {
                    throw;
                }
                return None.Default;
            }
        }
        Result<RobaloConfiguration, None> CheckConfiguration(Option<RobaloConfiguration> configurationOrNone)
        {
            if (configurationOrNone is Some<RobaloConfiguration> configuration)
            {
                if (configuration.Value.ConfigurationId == _currentConfigurationId)
                {
                    return None.Default;
                }

                try
                {
                    configuration.Value.Value.Verify();
                }
                catch (Exception exc)
                {
                    _logger.Error(exc, "Configuration {ConfigurationId} is not valid and cannot be loaded.", configuration.Value.ConfigurationId);
                    if (_currentConfigurationId.IsNone)
                    {
                        throw;
                    }
                    return None.Default;
                }
                return configuration.Value;
            }

            _logger.Error("Could not load configuration from the repository: No configuration found");
            if (_currentConfigurationId.IsNone)
            {
#pragma warning disable CA2201 // Do not raise reserved exception types
                throw new Exception("No configuration found");
#pragma warning restore CA2201 // Do not raise reserved exception types
            }
            return None.Default;
        }

        None LoadConfigurationToData(RobaloConfiguration configuration)
        {
            try
            {
                var json = JsonSerializer.Serialize(configuration.Value);
                using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));

                var parsed = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();

                Data = parsed.AsEnumerable()
                    .ToDictionary(x => $"Robalo:{x.Key}", x => x.Value, StringComparer.OrdinalIgnoreCase);

                OnReload();

                _logger.Information("Configuration {ConfigurationId} has been loaded", configuration.ConfigurationId);
                _currentConfigurationId = configuration.ConfigurationId;

                return None.Default;
            }
            catch (Exception exc)
            {
                _logger.Error(exc, "Configuration {ConfigurationId} could not be loaded into Data", configuration.ConfigurationId);
                if (_currentConfigurationId.IsNone)
                {
                    throw;
                }
                return None.Default;
            }
        }

    }

    public void Dispose()
    {
        _timer?.Dispose();
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await _timer.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}