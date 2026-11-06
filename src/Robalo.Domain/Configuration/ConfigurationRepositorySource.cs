using Microsoft.Extensions.Configuration;
using Robalo.Domain.Repositories;
using Serilog;

namespace Robalo.Domain.Configuration;

public sealed class ConfigurationRepositorySource(IConfigurationRepository repository) : IConfigurationSource
{
    public IConfigurationProvider Build(IConfigurationBuilder builder) =>
        new ConfigurationRepositoryProvider(Log.Logger, repository);
}