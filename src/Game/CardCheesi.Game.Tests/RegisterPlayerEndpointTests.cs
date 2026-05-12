extern alias PlayersApi;
using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using PlayersApi::CardCheesi.Players.Api.Endpoints;
using PlayersApi::CardCheesi.Players.Api.Features.RegisterPlayer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace CardCheesi.Game.Tests;

public class RegisterPlayerEndpointTests
{
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
        var handler = new Mock<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>>();
        handler.Setup(h => h.Handle(It.IsAny<RegisterPlayerCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RegisterPlayerResult("access-token", "raw-refresh-token"));

        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest("Alice");

        var result = await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, handler.Object, CreateJwtOptions(), CancellationToken.None);

        Assert.NotNull(result);
        handler.Verify(h => h.Handle(It.Is<RegisterPlayerCommand>(c => c.Name == "Alice"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_ValidName_SetsCookieOnResponse()
    {
        var handler = new Mock<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>>();
        handler.Setup(h => h.Handle(It.IsAny<RegisterPlayerCommand>(), It.IsAny<CancellationToken>()))
               .ReturnsAsync(new RegisterPlayerResult("access-token", "raw-refresh-token"));

        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest("Bob");

        await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, handler.Object, CreateJwtOptions(), CancellationToken.None);

        handler.Verify(h => h.Handle(It.Is<RegisterPlayerCommand>(c => c.Name == "Bob"), It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task HandleAsync_EmptyName_DoesNotCallHandler()
    {
        var handler = new Mock<ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult>>();
        var httpContext = CreateHttpContext();
        var request = new RegisterPlayerRequest(string.Empty);

        await RegisterPlayerEndpoint.HandleAsync(
            request, httpContext, handler.Object, CreateJwtOptions(), CancellationToken.None);

        handler.Verify(h => h.Handle(It.IsAny<RegisterPlayerCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}

