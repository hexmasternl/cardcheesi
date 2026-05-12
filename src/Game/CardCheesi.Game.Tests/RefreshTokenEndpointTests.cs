extern alias PlayersApi;
using CardCheesi.Auth;
using CardCheesi.Core;
using PlayersApi::CardCheesi.Players.Api.Endpoints;
using PlayersApi::CardCheesi.Players.Api.Features.RefreshToken;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace CardCheesi.Game.Tests;

public class RefreshTokenEndpointTests
{
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

    [Fact]
    public async Task HandleAsync_NoCookie_Returns401WithoutCallingHandler()
    {
        var handler = new Mock<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>>();
        var ctx = CreateHttpContextNoCookie();

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, handler.Object, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        handler.Verify(h => h.Handle(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_HandlerReturnsNull_Returns401()
    {
        var handler = new Mock<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>>();
        handler.Setup(h => h.Handle(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync((RefreshTokenResult?)null);

        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, "some-token");

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, handler.Object, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task HandleAsync_ValidToken_Returns200AndSetsCookie()
    {
        var handler = new Mock<ICommandHandler<RefreshTokenCommand, RefreshTokenResult?>>();
        handler.Setup(h => h.Handle(It.IsAny<RefreshTokenCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RefreshTokenResult("new-access-token", "new-raw-refresh-token"));

        var ctx = CreateHttpContextWithCookie(RegisterPlayerEndpoint.RefreshCookieName, "valid-token");

        var result = await RefreshTokenEndpoint.HandleAsync(ctx, handler.Object, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        handler.Verify(h => h.Handle(It.Is<RefreshTokenCommand>(c => c.RawCookieValue == "valid-token"), It.IsAny<CancellationToken>()), Times.Once);
    }
}

