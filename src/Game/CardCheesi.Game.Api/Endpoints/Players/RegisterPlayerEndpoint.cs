using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Api.Auth;
using CardCheesi.Game.Api.Features.RegisterPlayer;
using Microsoft.Extensions.Options;

namespace CardCheesi.Game.Api.Endpoints.Players;

public static class RegisterPlayerEndpoint
{
    internal const string RefreshCookieName = "cc_refresh";

    public static IEndpointRouteBuilder MapRegisterPlayer(this IEndpointRouteBuilder app)
    {
        app.MapPost("/players", HandleAsync)
            .WithName("RegisterPlayer")
            .Produces<RegisterPlayerResponse>(StatusCodes.Status201Created)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithOpenApi();

        return app;
    }

    internal static async Task<IResult> HandleAsync(
        RegisterPlayerRequest request,
        HttpContext httpContext,
        ICommandHandler<RegisterPlayerCommand, RegisterPlayerResult> handler,
        IOptions<JwtSettings> jwtOptions,
        CancellationToken ct)
    {
        var validationErrors = ValidateName(request.Name);
        if (validationErrors is not null)
            return Results.ValidationProblem(validationErrors);

        var result = await handler.Handle(new RegisterPlayerCommand(request.Name), ct);

        httpContext.Response.Cookies.Append(
            RefreshCookieName,
            result.RawRefreshToken,
            BuildRefreshCookieOptions(jwtOptions.Value));

        return Results.Created("/players", new RegisterPlayerResponse(result.AccessToken));
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

