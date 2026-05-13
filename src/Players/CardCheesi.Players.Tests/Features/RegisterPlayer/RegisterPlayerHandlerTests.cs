using CardCheesi.Auth;
using CardCheesi.Players.Features.RegisterPlayer;
using CardCheesi.Players.Persistence;
using CardCheesi.Players.Tests.Factories;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Tests.Features.RegisterPlayer;

public sealed class RegisterPlayerHandlerTests
{
    private static RegisterPlayerHandler CreateHandler(PlayersDbContext db, JwtSettings? settings = null)
    {
        var resolved = settings ?? JwtSettingsFactory.Create();
        var jwtService = new JwtTokenService(Options.Create(resolved));
        return new RegisterPlayerHandler(db, jwtService, Options.Create(resolved));
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesPlayerAndRefreshToken()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler = CreateHandler(db);

        var result = await handler.Handle(new RegisterPlayerCommand("Alice"), CancellationToken.None);

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RawRefreshToken);

        var player = Assert.Single(db.Players);
        Assert.Equal("Alice", player.Name);

        var refreshToken = Assert.Single(db.RefreshTokens);
        Assert.Equal(player.Id, refreshToken.PlayerId);
        Assert.Null(refreshToken.RevokedAt);
    }

    [Fact]
    public async Task Handle_ValidCommand_SetsCreatedAtAndLastSeenAt()
    {
        var before = DateTime.UtcNow;
        using var db = DbContextFactory.CreateInMemory();
        var handler = CreateHandler(db);

        await handler.Handle(new RegisterPlayerCommand("Bob"), CancellationToken.None);

        var player = Assert.Single(db.Players);
        Assert.True(player.CreatedAt >= before);
        Assert.True(player.LastSeenAt >= before);
    }

    [Fact]
    public async Task Handle_ValidCommand_RefreshTokenExpiresPerSettings()
    {
        using var db = DbContextFactory.CreateInMemory();
        var settings = JwtSettingsFactory.Create(refreshTokenExpiryDays: 7);
        var handler = CreateHandler(db, settings);

        var before = DateTime.UtcNow;
        await handler.Handle(new RegisterPlayerCommand("Carol"), CancellationToken.None);

        var refreshToken = Assert.Single(db.RefreshTokens);
        var expectedExpiry = before.AddDays(7);
        Assert.True(refreshToken.ExpiresAt >= expectedExpiry);
        Assert.True(refreshToken.ExpiresAt <= expectedExpiry.AddSeconds(5));
    }

    [Fact]
    public async Task Handle_ValidCommand_AccessTokenIsNonEmpty()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler = CreateHandler(db);

        var result = await handler.Handle(new RegisterPlayerCommand("Dave"), CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
    }

    [Fact]
    public async Task Handle_MultipleInvocations_EachPlayerIsIndependent()
    {
        using var db = DbContextFactory.CreateInMemory();
        var handler = CreateHandler(db);

        await handler.Handle(new RegisterPlayerCommand("Player1"), CancellationToken.None);
        await handler.Handle(new RegisterPlayerCommand("Player2"), CancellationToken.None);

        Assert.Equal(2, db.Players.Count());
        Assert.Equal(2, db.RefreshTokens.Count());
        Assert.All(db.Players, p => Assert.NotNull(p.Name));
    }
}
