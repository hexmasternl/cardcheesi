using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Players.Api.Features.RefreshToken;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Api.Endpoints;

public static class RefreshTokenEndpoint
{
    public static IEndpointRouteBuilder MapRefreshToken(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players/refresh", HandleAsync)
            .WithName("RefreshToken")
            .Produces<RefreshTokenResponse>()
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        return app;
    }

    internal static async Task<IResult> HandleAsync(
        HttpContext httpContext,
        ICommandHandler<RefreshTokenCommand, RefreshTokenResult?> handler,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var rawCookie = httpContext.Request.Cookies[RegisterPlayerEndpoint.RefreshCookieName];
        if (string.IsNullOrEmpty(rawCookie))
            return Results.Unauthorized();

        var result = await handler.Handle(new RefreshTokenCommand(rawCookie), ct);
        if (result is null)
            return Results.Unauthorized();

        httpContext.Response.Cookies.Append(
            RegisterPlayerEndpoint.RefreshCookieName,
            result.RawRefreshToken,
            RegisterPlayerEndpoint.BuildRefreshCookieOptions(jwtOptions.Value));

        return Results.Ok(new RefreshTokenResponse(result.AccessToken));
    }
}
