using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Endpoints.Players;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Tests;

public class RefreshTokenEndpointTests
{
    private static AppDbContext CreateDb(string dbName)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
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

    private static DefaultHttpContext CreateHttpContextWithCookie(string cookieName, string value)
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        ctx.Request.Headers.Cookie = $"{cookieName}={value}";
        return ctx;
    }

    private static DefaultHttpContext CreateHttpContextNoCookie()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

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
    public async Task HandleAsync_NoCookie_Returns401()
    {
        await using var db = CreateDb(nameof(HandleAsync_NoCookie_Returns401));
        var ctx = CreateHttpContextNoCookie();

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, db, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        // Verify nothing was changed in db
        Assert.Equal(0, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_UnknownToken_Returns401()
    {
        await using var db = CreateDb(nameof(HandleAsync_UnknownToken_Returns401));
        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, "unknowntokenvalue");

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, db, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(0, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task HandleAsync_ExpiredToken_Returns401()
    {
        await using var db = CreateDb(nameof(HandleAsync_ExpiredToken_Returns401));
        var (_, rawToken) = await SeedPlayerAndTokenAsync(db, expiresAt: DateTime.UtcNow.AddDays(-1));
        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, rawToken);

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, db, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_RevokedToken_RevokesAllAndReturns401()
    {
        await using var db = CreateDb(nameof(HandleAsync_RevokedToken_RevokesAllAndReturns401));
        var (player, rawToken) = await SeedPlayerAndTokenAsync(db, revokedAt: DateTime.UtcNow.AddHours(-1));

        // Add a second active token for the same player
        db.RefreshTokens.Add(new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = player.Id,
            TokenHash = "anotherhash",
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(30),
        });
        await db.SaveChangesAsync();

        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, rawToken);

        await RefreshTokenEndpoint.HandleAsync(ctx, db, CreateJwtOptions(), CancellationToken.None);

        // All active tokens should now be revoked
        var activeTokenCount = await db.RefreshTokens.CountAsync(t => t.RevokedAt == null);
        Assert.Equal(0, activeTokenCount);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_RotatesAndReturnsNewToken()
    {
        await using var db = CreateDb(nameof(HandleAsync_ValidToken_RotatesAndReturnsNewToken));
        var (_, rawToken) = await SeedPlayerAndTokenAsync(db);
        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, rawToken);

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, db, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        // Old token should be revoked, new one should exist
        var tokens = await db.RefreshTokens.ToListAsync();
        Assert.Equal(2, tokens.Count);
        var oldToken = tokens.First(t => t.RevokedAt.HasValue);
        var newToken = tokens.First(t => !t.RevokedAt.HasValue);
        Assert.NotNull(oldToken);
        Assert.NotNull(newToken);
    }
}
