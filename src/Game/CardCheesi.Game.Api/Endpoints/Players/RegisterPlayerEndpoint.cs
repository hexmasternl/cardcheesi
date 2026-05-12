using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Api.Endpoints.Players;

public record RegisterPlayerRequest(string Name);
public record RegisterPlayerResponse(string Token);

public static class RegisterPlayerEndpoint
{
    internal const string RefreshCookieName = "cc_refresh";

    public static IEndpointRouteBuilder MapRegisterPlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players", HandleAsync)
            .WithName("RegisterPlayer")
            .Produces<RegisterPlayerResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError);

        return app;
    }

    internal static async Task<IResult> HandleAsync(
        [FromBody] RegisterPlayerRequest request,
        HttpContext httpContext,
        AppDbContext db,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var validationErrors = ValidateName(request.Name);
        if (validationErrors is not null)
            return Results.ValidationProblem(validationErrors);

        var settings = jwtOptions.Value;
        var now = DateTime.UtcNow;
        var playerId = Guid.NewGuid();

        var player = new PlayerEntity
        {
            Id = playerId,
            Name = request.Name,
            CreatedAt = now,
            LastSeenAt = now,
        };

        db.Players.Add(player);

        var (rawToken, tokenHash) = JwtTokenService.GenerateRefreshToken();
        var refreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = playerId,
            TokenHash = tokenHash,
            CreatedAt = now,
            ExpiresAt = now.AddDays(settings.RefreshTokenExpiryDays),
        };

        db.RefreshTokens.Add(refreshToken);
        await db.SaveChangesAsync(ct);

        var accessToken = JwtTokenService.GenerateAccessToken(settings, playerId, request.Name);

        httpContext.Response.Cookies.Append(RefreshCookieName, rawToken, BuildRefreshCookieOptions(settings));

        return Results.Created("/players", new RegisterPlayerResponse(accessToken));
    }

    internal static CookieOptions BuildRefreshCookieOptions(JwtSettings settings) => new()
    {
        HttpOnly = true,
        Secure = settings.CookieSecure,
        SameSite = SameSiteMode.Strict,
        Path = "/players/refresh",
        MaxAge = TimeSpan.FromSeconds(2592000),
    };

    internal static Dictionary<string, string[]>? ValidateName(string? name)
    {
        if (string.IsNullOrEmpty(name))
            return new Dictionary<string, string[]> { ["name"] = ["Name is required."] };

        if (name != name.Trim())
            return new Dictionary<string, string[]> { ["name"] = ["Name must not have leading or trailing whitespace."] };

        if (name.Any(c => c < 0x20))
            return new Dictionary<string, string[]> { ["name"] = ["Name must not contain control characters."] };

        if (name.Length > 50)
            return new Dictionary<string, string[]> { ["name"] = [$"Name must not exceed 50 characters (was {name.Length})."] };

        return null;
    }
}
