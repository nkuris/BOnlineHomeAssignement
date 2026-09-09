using Microsoft.Extensions.Diagnostics.HealthChecks;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BOnlineHomeAssignement.Server.HealthChecks;

public class DbConnectionHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory;

    public DbConnectionHealthCheck(IDbContextFactory<AppDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = _dbFactory.CreateDbContext();
            if (await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Healthy("Database reachable");
            return HealthCheckResult.Unhealthy("Database cannot be reached");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception while checking database connectivity", ex);
        }
    }
}
