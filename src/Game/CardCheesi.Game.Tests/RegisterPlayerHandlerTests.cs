using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Features.RegisterPlayer;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Tests;

public class RegisterPlayerHandlerTests
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

    [Fact]
    public async Task Handle_ValidName_CreatesPlayerAndToken()
    {
        await using var db = CreateDb();
        var handler = new RegisterPlayerHandler(db, CreateJwtOptions());

        var result = await handler.Handle(new RegisterPlayerCommand("Alice"), CancellationToken.None);

        Assert.NotNull(result.AccessToken);
        Assert.NotNull(result.RawRefreshToken);
        Assert.Equal(1, await db.Players.CountAsync());
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
    }

    [Fact]
    public async Task Handle_CreatesPlayerWithCorrectName()
    {
        await using var db = CreateDb();
        var handler = new RegisterPlayerHandler(db, CreateJwtOptions());

        await handler.Handle(new RegisterPlayerCommand("Bob"), CancellationToken.None);

        var player = await db.Players.FirstAsync();
        Assert.Equal("Bob", player.Name);
    }

    [Fact]
    public async Task Handle_ReturnsNonEmptyAccessToken()
    {
        await using var db = CreateDb();
        var handler = new RegisterPlayerHandler(db, CreateJwtOptions());

        var result = await handler.Handle(new RegisterPlayerCommand("Charlie"), CancellationToken.None);

        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RawRefreshToken);
    }
}
