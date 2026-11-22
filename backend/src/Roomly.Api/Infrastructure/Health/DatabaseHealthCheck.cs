using Microsoft.Extensions.Diagnostics.HealthChecks;
using Roomly.Api.Infrastructure.Persistence;

namespace Roomly.Api.Infrastructure.Health;

internal sealed class DatabaseHealthCheck(AppDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            return await db.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy()
                : HealthCheckResult.Unhealthy("Veritabanına bağlanılamıyor.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Veritabanına bağlanılamıyor.", ex);
        }
    }
}
