using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Endpoints.Players;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Tests;

public class RegisterPlayerEndpointTests
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static IOptions<JwtSettings> CreateJwtOptions() =>
        Options.Create(new JwtSettings
        {
            SigningKey = "test-signing-key-that-is-at-least-32-bytes",
            Issuer = "test-issuer",
            Audience = "test-audience",
            CookieSecure = false,
        });

    private static DefaultHttpContext CreateHttpContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    [Fact]
    public async Task HandleAsync_ValidName_Returns201WithToken()
    {
        await using var db = CreateDb();
        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest("Alice");

        var result = await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, db, CreateJwtOptions(), CancellationToken.None);

        var created = Assert.IsAssignableFrom<IResult>(result);
        Assert.NotNull(created);
        Assert.Equal(1, await db.Players.CountAsync());
        Assert.Equal(1, await db.RefreshTokens.CountAsync());
        Assert.True(httpContext.Response.Cookies is not null);
    }

    [Fact]
    public async Task HandleAsync_ValidName_SetsCookieAndReturnsJwt()
    {
        await using var db = CreateDb();
        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest("Bob");

        await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, db, CreateJwtOptions(), CancellationToken.None);

        var player = await db.Players.FirstAsync();
        Assert.Equal("Bob", player.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void ValidateName_EmptyOrNull_ReturnsError(string? name)
    {
        var errors = RegisterPlayerEndpoint.ValidateName(name);

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateName_WithLeadingWhitespace_ReturnsError()
    {
        var errors = RegisterPlayerEndpoint.ValidateName(" Alice");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateName_WithTrailingWhitespace_ReturnsError()
    {
        var errors = RegisterPlayerEndpoint.ValidateName("Alice ");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateName_NameExceeds50Chars_ReturnsError()
    {
        var longName = new string('x', 51);

        var errors = RegisterPlayerEndpoint.ValidateName(longName);

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Fact]
    public void ValidateName_NameWithControlCharacter_ReturnsError()
    {
        var errors = RegisterPlayerEndpoint.ValidateName("Alice\x01");

        Assert.NotNull(errors);
        Assert.True(errors.ContainsKey("name"));
    }

    [Theory]
    [InlineData("Alice")]
    [InlineData("Player 123")]
    [InlineData("Ünïcödé")]
    public void ValidateName_ValidName_ReturnsNull(string name)
    {
        var errors = RegisterPlayerEndpoint.ValidateName(name);

        Assert.Null(errors);
    }

    [Fact]
    public async Task HandleAsync_EmptyName_Returns400()
    {
        await using var db = CreateDb();
        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest(string.Empty);

        var result = await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, db, CreateJwtOptions(), CancellationToken.None);

        // Should not have saved anything
        Assert.Equal(0, await db.Players.CountAsync());
    }
}
