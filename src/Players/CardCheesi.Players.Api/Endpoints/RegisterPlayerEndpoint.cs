using CardCheesi.Auth;
using CardCheesi.Core;
using CardCheesi.Players.Abstractions.DataTransferObjects;
using CardCheesi.Players.Features.RegisterPlayer;
using CardCheesi.Players.Validators;
using Microsoft.Extensions.Options;

namespace CardCheesi.Players.Api.Endpoints;

public static class RegisterPlayerEndpoint
{
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
        var validationErrors = PlayerNameValidator.Validate(request.Name);
        if (validationErrors is not null)
            return Results.ValidationProblem(validationErrors);

        var result = await handler.Handle(new RegisterPlayerCommand(request.Name), ct);

        httpContext.Response.Cookies.Append(
            CookieHelper.RefreshCookieName,
            result.RawRefreshToken,
            CookieHelper.BuildRefreshCookieOptions(jwtOptions.Value));

        return Results.Created("/players", new RegisterPlayerResponse(result.AccessToken));
    }
}
