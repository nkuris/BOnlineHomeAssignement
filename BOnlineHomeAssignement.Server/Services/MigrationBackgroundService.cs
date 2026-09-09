using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using BOnlineHomeAssignement.Server.Infrastructure.Persistence;

namespace BOnlineHomeAssignement.Server.Services
{
    // Runs EF Core migrations in background so the web host can start quickly and Swagger/UI is available.
    public class MigrationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<MigrationBackgroundService> _logger;

        public MigrationBackgroundService(IServiceProvider services, ILogger<MigrationBackgroundService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                _logger.LogInformation("MigrationBackgroundService starting - will run migrations in background");

                // read env-driven configuration for retries/delay
                var retryCount = 10;
                try { var rc = Environment.GetEnvironmentVariable("MIGRATIONS__RETRY_COUNT"); if (!string.IsNullOrWhiteSpace(rc)) retryCount = int.Parse(rc); } catch { }
                var delaySeconds = 5;
                try { var ds = Environment.GetEnvironmentVariable("MIGRATIONS__RETRY_DELAY_SECONDS"); if (!string.IsNullOrWhiteSpace(ds)) delaySeconds = int.Parse(ds); } catch { }

                var scopeFactory = _services.GetService(typeof(IServiceScopeFactory)) as IServiceScopeFactory;
                if (scopeFactory == null)
                {
                    _logger.LogWarning("MigrationBackgroundService: IServiceScopeFactory not available from services");
                    return;
                }

                using var s = scopeFactory.CreateScope();
                var sp = s.ServiceProvider;

                try
                {
                    await sp.MigrateAllDbContextsWithRetryAsync(retryCount);
                    _logger.LogInformation("MigrationBackgroundService: migrations completed");

                    // If AppDbContext is registered, run seed
                    // NOTE: Seeding is temporarily disabled because the configurable workflow migrations are no-ops.
                    // Once the workflow migrations are properly authored, re-enable this.
                    try
                    {
                        var db = sp.GetService(typeof(BOnlineHomeAssignement.Server.Infrastructure.Persistence.AppDbContext)) as BOnlineHomeAssignement.Server.Infrastructure.Persistence.AppDbContext;
                        if (db != null)
                        {
                            // TODO: Re-enable once workflow migrations are complete
                            // BOnlineHomeAssignement.Server.Infrastructure.Persistence.SeedData.Initialize(db);
                            _logger.LogInformation("MigrationBackgroundService: seed skipped (workflow migrations incomplete)");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "MigrationBackgroundService: seeding failed");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "MigrationBackgroundService: migrations failed");
                    try
                    {
                        // If email service available, attempt to notify admin (best-effort)
                        var mailer = sp.GetService(typeof(object));
                        // no-op: keep behavior minimal
                    }
                    catch { }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "MigrationBackgroundService unexpected error");
            }
        }
    }
}
