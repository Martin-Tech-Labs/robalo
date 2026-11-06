using Robalo.Common.Models;
using Robalo.Domain.Models.Configuration;

namespace Robalo.Domain.Repositories;

public interface IConfigurationRepository
{
    Task<Option<RobaloConfiguration>> LoadConfiguration(CancellationToken cancellationToken);
    Task<int> SaveConfiguration(RobaloOptions value, DateTimeOffset createdOn, CancellationToken cancellationToken);
}