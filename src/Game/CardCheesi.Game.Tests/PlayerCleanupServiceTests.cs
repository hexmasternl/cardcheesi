using CardCheesi.Game.Api;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace CardCheesi.Game.Tests;

public class PlayerCleanupServiceTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        return new AppDbContext(options);
    }

    private static PlayerCleanupService CreateService(AppDbContext db)
    {
        var services = new ServiceCollection();
        services.AddSingleton(db);
        services.AddScoped<AppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        var serviceProvider = services.BuildServiceProvider();

        var config = new ConfigurationBuilder().Build();
        var logger = NullLogger<PlayerCleanupService>.Instance;

        return new PlayerCleanupService(serviceProvider.GetRequiredService<IServiceScopeFactory>(), config, logger);
    }

    [Fact]
    public async Task RunSweepAsync_InactivePlayer_GetsDeleted()
    {
        var dbName = nameof(RunSweepAsync_InactivePlayer_GetsDeleted);
        await using var db = CreateDb(dbName);

        var inactivePlayer = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = "InactivePlayer",
            CreatedAt = DateTime.UtcNow.AddDays(-40),
            LastSeenAt = DateTime.UtcNow.AddDays(-35),
        };
        db.Players.Add(inactivePlayer);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Equal(0, await db.Players.CountAsync());
    }

    [Fact]
    public async Task RunSweepAsync_ActivePlayer_IsRetained()
    {
        var dbName = nameof(RunSweepAsync_ActivePlayer_IsRetained);
        await using var db = CreateDb(dbName);

        var activePlayer = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = "ActivePlayer",
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            LastSeenAt = DateTime.UtcNow.AddDays(-1),
        };
        db.Players.Add(activePlayer);
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Equal(1, await db.Players.CountAsync());
    }

    [Fact]
    public async Task RunSweepAsync_ExpiredToken_GetsDeleted()
    {
        var dbName = nameof(RunSweepAsync_ExpiredToken_GetsDeleted);
        await using var db = CreateDb(dbName);

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Player",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastSeenAt = DateTime.UtcNow.AddDays(-1),
        };
        db.Players.Add(player);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = "expiredhash",
            CreatedAt = DateTime.UtcNow.AddDays(-31),
            ExpiresAt = DateTime.UtcNow.AddDays(-1),
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Equal(0, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RunSweepAsync_ActiveToken_IsRetained()
    {
        var dbName = nameof(RunSweepAsync_ActiveToken_IsRetained);
        await using var db = CreateDb(dbName);

        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = "Player",
            CreatedAt = DateTime.UtcNow.AddDays(-1),
            LastSeenAt = DateTime.UtcNow.AddDays(-1),
        };
        db.Players.Add(player);

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = "activehash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(29),
        });
        await db.SaveChangesAsync();

        var service = CreateService(db);
        await service.RunSweepAsync(CancellationToken.None);

        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task RunSweepAsync_EmptyDatabase_DoesNotThrow()
    {
        var dbName = nameof(RunSweepAsync_EmptyDatabase_DoesNotThrow);
        await using var db = CreateDb(dbName);

        var service = CreateService(db);

        var exception = await Record.ExceptionAsync(() =>
            service.RunSweepAsync(CancellationToken.None));

        Assert.Null(exception);
    }
}
