using CardCheesi.Players.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CardCheesi.Players;

public sealed class PlayerCleanupService(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<PlayerCleanupService> logger) : BackgroundService
{
    private TimeSpan SweepInterval =>
        TimeSpan.FromHours(configuration.GetValue<double>("Cleanup:IntervalHours", 24));

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(SweepInterval, stoppingToken);
            await RunSweepAsync(stoppingToken);
        }
    }

    internal async Task RunSweepAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PlayersDbContext>();

        var now = DateTime.UtcNow;
        var inactiveCutoff = now.AddDays(-31);

        int expiredTokensDeleted;
        int inactivePlayersDeleted;

        if (db.Database.IsRelational())
        {
            expiredTokensDeleted = await db.RefreshTokens
                .Where(t => t.ExpiresAt < now)
                .ExecuteDeleteAsync(ct);

            inactivePlayersDeleted = await db.Players
                .Where(p => p.LastSeenAt < inactiveCutoff)
                .ExecuteDeleteAsync(ct);
        }
        else
        {
            var expiredTokens = await db.RefreshTokens
                .Where(t => t.ExpiresAt < now)
                .ToListAsync(ct);
            db.RefreshTokens.RemoveRange(expiredTokens);
            expiredTokensDeleted = expiredTokens.Count;

            var inactivePlayers = await db.Players
                .Where(p => p.LastSeenAt < inactiveCutoff)
                .ToListAsync(ct);
            db.Players.RemoveRange(inactivePlayers);
            inactivePlayersDeleted = inactivePlayers.Count;

            await db.SaveChangesAsync(ct);
        }

        logger.LogInformation(
            "Player cleanup complete: {PlayersDeleted} player(s) and {TokensDeleted} token(s) removed.",
            inactivePlayersDeleted,
            expiredTokensDeleted);
    }
}
