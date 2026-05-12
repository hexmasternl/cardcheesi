using CardCheesi.Game.Persistence;
using CardCheesi.Players.Features.RefreshToken;
using CardCheesi.Players.Tests.Factories;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Tests.Features.RefreshToken;

public sealed class RefreshTokenHandlerTests
{
    [Fact]
    public async Task Handle_TokenNotFound_ReturnsNull()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));

        var result = await handler.Handle(new RefreshTokenCommand("nonexistent_token"), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_ValidToken_RotatesAndReturnsNewTokens()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create();
        db.Players.Add(player);
        var (rawToken, tokenEntity) = RefreshTokenFactory.CreateWithRawToken(playerId: player.Id);
        tokenEntity.Player = player;
        db.RefreshTokens.Add(tokenEntity);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RawRefreshToken);
        Assert.NotEqual(rawToken, result.RawRefreshToken);
    }

    [Fact]
    public async Task Handle_ValidToken_RevokesOldToken()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create();
        db.Players.Add(player);
        var (rawToken, tokenEntity) = RefreshTokenFactory.CreateWithRawToken(playerId: player.Id);
        tokenEntity.Player = player;
        db.RefreshTokens.Add(tokenEntity);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));
        await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        var originalToken = db.RefreshTokens.Single(t => t.Id == tokenEntity.Id);
        Assert.NotNull(originalToken.RevokedAt);
    }

    [Fact]
    public async Task Handle_ValidToken_CreatesNewRefreshToken()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create();
        db.Players.Add(player);
        var (rawToken, tokenEntity) = RefreshTokenFactory.CreateWithRawToken(playerId: player.Id);
        tokenEntity.Player = player;
        db.RefreshTokens.Add(tokenEntity);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));
        await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Equal(2, db.RefreshTokens.Count());
        var newToken = db.RefreshTokens.Single(t => t.Id != tokenEntity.Id);
        Assert.Equal(player.Id, newToken.PlayerId);
        Assert.Null(newToken.RevokedAt);
    }

    [Fact]
    public async Task Handle_ExpiredToken_ReturnsNull()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create();
        db.Players.Add(player);
        var (rawToken, tokenEntity) = RefreshTokenFactory.CreateWithRawToken(
            playerId: player.Id,
            expiresAt: DateTime.UtcNow.AddDays(-1));
        tokenEntity.Player = player;
        db.RefreshTokens.Add(tokenEntity);
        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));

        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task Handle_AlreadyRevokedToken_RevokesAllActiveTokensAndReturnsNull()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create();
        db.Players.Add(player);

        // Stolen (already revoked) token
        var (rawToken, stolenToken) = RefreshTokenFactory.CreateWithRawToken(
            playerId: player.Id,
            revokedAt: DateTime.UtcNow.AddMinutes(-10));
        stolenToken.Player = player;
        db.RefreshTokens.Add(stolenToken);

        // Active token issued after the theft
        var activeToken = RefreshTokenFactory.Create(
            playerId: player.Id,
            expiresAt: DateTime.UtcNow.AddDays(30));
        activeToken.Player = player;
        db.RefreshTokens.Add(activeToken);

        await db.SaveChangesAsync();

        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));
        var result = await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        Assert.Null(result);

        var tokens = db.RefreshTokens.ToList();
        Assert.All(tokens.Where(t => t.Id != stolenToken.Id), t => Assert.NotNull(t.RevokedAt));
    }

    [Fact]
    public async Task Handle_ValidToken_UpdatesPlayerLastSeenAt()
    {
        using var db = DbContextFactory.CreateInMemory();
        var player = PlayerFactory.Create(lastSeenAt: DateTime.UtcNow.AddHours(-1));
        db.Players.Add(player);
        var (rawToken, tokenEntity) = RefreshTokenFactory.CreateWithRawToken(playerId: player.Id);
        tokenEntity.Player = player;
        db.RefreshTokens.Add(tokenEntity);
        await db.SaveChangesAsync();

        var before = DateTime.UtcNow;
        var handler = new RefreshTokenHandler(db, Options.Create(JwtSettingsFactory.Create()));
        await handler.Handle(new RefreshTokenCommand(rawToken), CancellationToken.None);

        var updatedPlayer = db.Players.Single(p => p.Id == player.Id);
        Assert.True(updatedPlayer.LastSeenAt >= before);
    }
}
