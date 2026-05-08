using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Api;

/// <summary>
/// Runs EF Core migrations against the database in the background when the application starts,
/// so the web host is not blocked and the health endpoint can respond during migration.
/// </summary>
public sealed class DatabaseMigrationWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<DatabaseMigrationWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Applying database migrations…");
        try
        {
            using var scope = scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (db.Database.IsRelational())
                await db.Database.MigrateAsync(stoppingToken);
            else
                await db.Database.EnsureCreatedAsync(stoppingToken);

            logger.LogInformation("Database migrations applied successfully.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database migration failed.");
            throw;
        }
    }
}
