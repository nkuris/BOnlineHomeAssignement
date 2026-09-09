using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace BOnlineHomeAssignement.Server.Infrastructure.Persistence
{
    public static class MigrationExtensions
    {
        private static Task LogMigrationAsync(string message, ILogger logger)
        {
            try { logger?.LogInformation("[Migration] {Message}", message); } catch { }
            return Task.CompletedTask;
        }

        /// <summary>
        /// Ensures the database exists by connecting to master and creating it if needed.
        /// This solves the problem where MigrateAsync fails with error 4060 (database doesn't exist).
        /// </summary>
        public static async Task EnsureDatabaseExistsAsync(this DbContext db)
        {
            var loggerFactory = ((IInfrastructure<IServiceProvider>)db).Instance.GetService(typeof(ILoggerFactory)) as ILoggerFactory ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger("EFMigrations");

            try
            {
                var conn = db.Database.GetDbConnection();
                if (conn is SqlConnection sqlConn)
                {
                    var connString = sqlConn.ConnectionString;
                    logger.LogInformation("Original connection string: Server={server}", new SqlConnectionStringBuilder(connString).DataSource);

                    var builder = new SqlConnectionStringBuilder(connString);
                    var databaseName = builder.InitialCatalog;

                    if (string.IsNullOrWhiteSpace(databaseName))
                    {
                        logger.LogWarning("Database name not found in connection string");
                        return;
                    }

                    logger.LogInformation("Ensuring database '{DatabaseName}' exists", databaseName);

                    // Build connection string to master database
                    builder.InitialCatalog = "master";
                    var masterConnString = builder.ConnectionString;
                    logger.LogInformation("Using master connection to check/create database");

                    using (var masterConn = new SqlConnection(masterConnString))
                    {
                        await masterConn.OpenAsync();
                        logger.LogInformation("Connected to master database successfully");

                        // Check if database exists
                        using (var cmd = masterConn.CreateCommand())
                        {
                            cmd.CommandText = $"SELECT COUNT(*) FROM sys.databases WHERE name = N'{databaseName.Replace("'", "''")}'";
                            var result = await cmd.ExecuteScalarAsync();

                            if (result != null && (int)result > 0)
                            {
                                logger.LogInformation("Database '{DatabaseName}' already exists", databaseName);
                                return;
                            }
                        }

                        // Create database if it doesn't exist
                        logger.LogInformation("Creating database '{DatabaseName}'...", databaseName);
                        using (var cmd = masterConn.CreateCommand())
                        {
                            cmd.CommandText = $"CREATE DATABASE [{databaseName}]";
                            cmd.CommandTimeout = 120;  // Allow more time for database creation
                            await cmd.ExecuteNonQueryAsync();
                            logger.LogInformation("Database '{DatabaseName}' created successfully", databaseName);
                        }
                    }
                }
                else
                {
                    logger.LogWarning("Database connection is not a SqlConnection, skipping database creation");
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error ensuring database exists (this may be expected if running against existing DB): {Message}", ex.Message);
                // Don't throw - allow the migration to proceed and retry
            }
        }

        public static async Task MigrateWithRetryAsync(this DbContext db, int retries = 10)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));

            var delay = TimeSpan.FromSeconds(5);
            var loggerFactory = ((IInfrastructure<IServiceProvider>)db).Instance.GetService(typeof(ILoggerFactory)) as ILoggerFactory ?? NullLoggerFactory.Instance;
            var logger = loggerFactory.CreateLogger("EFMigrations");
            var migrator = ((IInfrastructure<IServiceProvider>)db).Instance.GetService(typeof(IMigrator)) as IMigrator;

            // First, ensure the database exists
            logger.LogInformation("Pre-migration: Ensuring database exists for {DbContext}", db.GetType().Name);
            await db.EnsureDatabaseExistsAsync();
            logger.LogInformation("Pre-migration: Database check completed for {DbContext}", db.GetType().Name);

            for (int attempt = 0; attempt < retries; attempt++)
            {
                try
                {
                    var pending = (await db.Database.GetPendingMigrationsAsync()).ToList();
                    if (!pending.Any())
                    {
                        logger.LogInformation("No pending migrations for {DbContext}", db.GetType().Name);
                        await LogMigrationAsync($"No pending migrations for {db.GetType().Name}", logger);
                        return;
                    }

                    logger.LogInformation("Applying {Count} pending migrations for {DbContext}: {Migrations}", pending.Count, db.GetType().Name, string.Join(", ", pending));
                    await LogMigrationAsync($"Pending {pending.Count} migrations for {db.GetType().Name}: {string.Join(", ", pending)}", logger);

                    // Use the standard Database.MigrateAsync which is more reliable and handles migration discovery better
                    logger.LogWarning("Using Database.MigrateAsync for reliable migration discovery");
                    await LogMigrationAsync($"Using Database.MigrateAsync to apply migrations", logger);
                    await db.Database.MigrateAsync();
                    logger.LogInformation("Database.MigrateAsync completed for {DbContext}", db.GetType().Name);
                    await LogMigrationAsync($"Database.MigrateAsync completed for {db.GetType().Name}", logger);
                    return;
                }
                catch (Exception ex)
                {
                    if (attempt == retries - 1)
                    {
                        logger.LogError(ex, "Migration failed permanently for {DbContext} after {Attempts} attempts", db.GetType().Name, retries);
                        await LogMigrationAsync($"Migration permanently failed for {db.GetType().Name} after {retries} attempts: {ex.Message}", logger);
                        throw;
                    }

                    logger.LogWarning(ex, "Migration attempt {Attempt} failed for {DbContext}, retrying in {DelaySeconds} seconds...", attempt + 1, db.GetType().Name, delay.TotalSeconds);
                    await LogMigrationAsync($"Migration attempt {attempt + 1} failed for {db.GetType().Name}: {ex.Message}. Retrying in {delay.TotalSeconds}s", logger);
                    await Task.Delay(delay);
                }
            }
        }

        public static async Task MigrateAllDbContextsWithRetryAsync(this IServiceProvider services, int retries = 10)
        {
            if (services == null) throw new ArgumentNullException(nameof(services));

            var dbContextTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => { try { return a.GetTypes(); } catch { return Array.Empty<Type>(); } })
                .Where(t => typeof(DbContext).IsAssignableFrom(t) && !t.IsAbstract)
                .ToList();

            foreach (var ctxType in dbContextTypes)
            {
                try
                {
                    var db = services.GetService(ctxType) as DbContext;
                    if (db == null)
                        continue;

                    await db.MigrateWithRetryAsync(retries);
                }
                catch (Exception ex)
                {
                    try
                    {
                        var mailer = services.GetService(typeof(object)); // placeholder, keep silent if not present
                    }
                    catch { }

                    throw new InvalidOperationException($"Failed to migrate DbContext '{ctxType.FullName}'.", ex);
                }
            }
        }
    }
}
