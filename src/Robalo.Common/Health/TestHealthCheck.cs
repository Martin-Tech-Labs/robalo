using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Robalo.Common.Health;

public class TestHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("OK"));
    }
}