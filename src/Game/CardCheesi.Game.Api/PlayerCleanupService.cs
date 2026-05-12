using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CardCheesi.Game.Api;

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
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;
        var inactiveCutoff = now.AddDays(-31);

        var expiredTokensDeleted = await db.RefreshTokens
            .Where(t => t.ExpiresAt < now)
            .ExecuteDeleteAsync(ct);

        var inactivePlayersDeleted = await db.Players
            .Where(p => p.LastSeenAt < inactiveCutoff)
            .ExecuteDeleteAsync(ct);

        logger.LogInformation(
            "Player cleanup complete: {PlayersDeleted} player(s) and {TokensDeleted} token(s) removed.",
            inactivePlayersDeleted,
            expiredTokensDeleted);
    }
}
