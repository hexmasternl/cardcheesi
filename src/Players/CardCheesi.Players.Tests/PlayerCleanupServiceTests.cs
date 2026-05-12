using CardCheesi.Players.Tests.Factories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CardCheesi.Players.Tests;

public sealed class PlayerCleanupServiceTests
{
    private static PlayerCleanupService CreateService(
        IServiceScopeFactory scopeFactory,
        IConfiguration? configuration = null)
    {
        configuration ??= new ConfigurationBuilder().Build();
        return new PlayerCleanupService(
            scopeFactory,
            configuration,
            NullLogger<PlayerCleanupService>.Instance);
    }

    [Fact]
    public async Task RunSweepAsync_RemovesExpiredRefreshTokens()
    {
        var db = DbContextFactory.CreateInMemory();

        var player = PlayerFactory.Create(lastSeenAt: DateTime.UtcNow);
        db.Players.Add(player);

        var expiredToken = RefreshTokenFactory.Create(
            playerId: player.Id,
            expiresAt: DateTime.UtcNow.AddDays(-1));
        expiredToken.Player = player;

        var validToken = RefreshTokenFactory.Create(
            playerId: player.Id,
            expiresAt: DateTime.UtcNow.AddDays(30));
        validToken.Player = player;

        db.RefreshTokens.Add(expiredToken);
        db.RefreshTokens.Add(validToken);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        var provider = services.BuildServiceProvider();

        var service = CreateService(provider.GetRequiredService<IServiceScopeFactory>());
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Single(db.RefreshTokens);
        Assert.Equal(validToken.Id, db.RefreshTokens.Single().Id);
    }

    [Fact]
    public async Task RunSweepAsync_RemovesInactivePlayers()
    {
        var db = DbContextFactory.CreateInMemory();

        var inactivePlayer = PlayerFactory.Create(
            lastSeenAt: DateTime.UtcNow.AddDays(-32));
        var activePlayer = PlayerFactory.Create(
            lastSeenAt: DateTime.UtcNow.AddDays(-5));

        db.Players.AddRange(inactivePlayer, activePlayer);
        await db.SaveChangesAsync();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        var provider = services.BuildServiceProvider();

        var service = CreateService(provider.GetRequiredService<IServiceScopeFactory>());
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Single(db.Players);
        Assert.Equal(activePlayer.Id, db.Players.Single().Id);
    }

    [Fact]
    public async Task RunSweepAsync_WhenNothingToClean_DoesNotThrow()
    {
        var db = DbContextFactory.CreateInMemory();

        var services = new ServiceCollection();
        services.AddSingleton(db);
        var provider = services.BuildServiceProvider();

        var service = CreateService(provider.GetRequiredService<IServiceScopeFactory>());

        var exception = await Record.ExceptionAsync(
            () => service.RunSweepAsync(CancellationToken.None));

        Assert.Null(exception);
    }
}
