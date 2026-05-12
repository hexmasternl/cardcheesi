using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Features.RefreshToken;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Tests;

public class RefreshTokenHandlerTests
{
    private static AppDbContext CreateDb(string? name = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(name ?? Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IOptions<JwtSettings> CreateJwtOptions() =>
        Options.Create(new JwtSettings
        {
            SigningKey = "test-signing-key-that-is-at-least-32-bytes",
            Issuer = "test-issuer",
            Audience = "test-audience",
            RefreshTokenExpiryDays = 30,
            CookieSecure = false,
        });

    private static async Task<(PlayerEntity player, string rawToken)> SeedPlayerAndTokenAsync(
        AppDbContext db, DateTime? expiresAt = null, DateTime? revokedAt = null)
    {
        var player = new PlayerEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestPlayer",
            CreatedAt = DateTime.UtcNow,
            LastSeenAt = DateTime.UtcNow,
        };
        db.Players.Add(player);

        var (rawToken, hash) = JwtTokenService.GenerateRefreshToken();
        var token = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = hash,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiresAt ?? DateTime.UtcNow.AddDays(30),
            RevokedAt = revokedAt,
        };
        db.RefreshTokens.Add(token);
        await db.SaveChangesAsync();

        return (player, rawToken);
    }

    [Fact]
    public async Task Handle_UnknownToken_ReturnsNull()
    {
        await using var db = CreateDb();
        var handler = new RefreshTokenHandler(db, CreateJwtOptions());

        var result = await handler.Handle(new RefreshTokenCommand("unknown-token"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsNull()
    {
        await using var db = CreateDb();
        var (_, rawToken) = await SeedPlayerAndTokenAsync(db, expiresAt: DateTime.UtcNow.AddDays(-1));
        var handler = new RefreshTokenHandler(db, CreateJwtOptions());

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_RevokedToken_RevokesAllActiveTokensAndReturnsNull()
    {
        await using var db = CreateDb();
        var (player, rawToken) = await SeedPlayerAndTokenAsync(db, revokedAt: DateTime.UtcNow.AddHours(-1));

        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = "anotherhash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, CreateJwtOptions());
        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Null(result);
        var activeCount = await db.RefreshTokens.CountAsync(t => t.RevokedAt == null);
        Assert.Equal(0, activeCount);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesTokenAndReturnsResult()
    {
        await using var db = CreateDb();
        var (_, rawToken) = await SeedPlayerAndTokenAsync(db);
        var handler = new RefreshTokenHandler(db, CreateJwtOptions());

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RawRefreshToken);

        var tokens = await db.RefreshTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        Assert.Single(tokens, t => t.RevokedAt.HasValue);
        Assert.Single(tokens, t => !t.RevokedAt.HasValue);
    }
}
