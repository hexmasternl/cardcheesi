using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Api.Endpoints.Players;

public record RefreshTokenResponse(string Token);

public static class RefreshTokenEndpoint
{
    public static IEndpointRouteBuilder MapRefreshToken(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/refresh", HandleAsync)
            .WithName("RefreshToken")
            .Produces<RefreshTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        return app;
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        AppDbContext db,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var rawCookie = httpContext.Request.Cookies[RegisterPlayerEndpoint.RefreshCookieName];
        if (string.IsNullOrEmpty(rawCookie))
            return Results.Unauthorized();

        var tokenHash = JwtTokenService.ComputeSha256Hex(rawCookie);
        var existingToken = await db.RefreshTokens
            .Include(t => t.Player)
            .FirstOrDefaultAsync(t => t.TokenHash == tokenHash, ct);

        if (existingToken is null)
            return Results.Unauthorized();

        if (existingToken.RevokedAt.HasValue)
        {
            // Theft detection: revoke all active tokens for this player
            var allActiveTokens = await db.RefreshTokens
                .Where(t => t.PlayerId == existingToken.PlayerId && t.RevokedAt == null)
                .ToListAsync(ct);

            var now = DateTime.UtcNow;
            foreach (var token in allActiveTokens)
                token.RevokedAt = now;

            await db.SaveChangesAsync(ct);
            return Results.Unauthorized();
        }

        if (existingToken.ExpiresAt < DateTime.UtcNow)
            return Results.Unauthorized();

        var settings = jwtOptions.Value;
        var utcNow = DateTime.UtcNow;

        existingToken.RevokedAt = utcNow;
        existingToken.Player.LastSeenAt = utcNow;

        var (rawToken, hash) = JwtTokenService.GenerateRefreshToken();
        var newRefreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            PlayerId = existingToken.PlayerId,
            TokenHash = hash,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddDays(settings.RefreshTokenExpiryDays),
        };

        db.RefreshTokens.Add(newRefreshToken);
        await db.SaveChangesAsync(ct);

        var accessToken = JwtTokenService.GenerateAccessToken(
            settings,
            existingToken.PlayerId,
            existingToken.Player.Name);

        httpContext.Response.Cookies.Append(
            RegisterPlayerEndpoint.RefreshCookieName,
            rawToken,
            RegisterPlayerEndpoint.BuildRefreshCookieOptions(settings));

        return Results.Ok(new RefreshTokenResponse(accessToken));
    }
}
