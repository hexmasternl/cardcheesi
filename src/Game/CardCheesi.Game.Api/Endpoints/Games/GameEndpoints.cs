using System.Security.Claims;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Features.CreateGame;
using CardCheesi.Game.Features.GetGame;
using CardCheesi.Game.Features.JoinGame;

namespace CardCheesi.Game.Api.Endpoints.Games;

public static class GameEndpoints
{
    public static IEndpointRouteBuilder MapGameEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/games").WithTags("Games");

        group.MapPost("/", CreateGame)
            .WithName("CreateGame")
            .RequireAuthorization()
            .Produces<CreateGameResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable)
            .WithOpenApi();

        group.MapGet("/{code}", GetGame)
            .WithName("GetGame")
            .RequireAuthorization()
            .Produces<GameDto>()
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status403Forbidden)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapGet("/{code}/events", GetGameEvents)
            .WithName("GameEvents");

        group.MapPost("/{code}/leave", LeaveGame)
            .WithName("LeaveGame")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .WithOpenApi();

        group.MapPost("/{code}/join", JoinGame)
            .WithName("JoinGame")
            .RequireAuthorization()
            .Produces<JoinGameResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .WithOpenApi();

        return app;
    }

    private static async Task<IResult> CreateGame(
        HttpContext httpContext,
        ICommandHandler<CreateGameCommand, CreateGameResult> handler,
        CancellationToken ct)
    {
        var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.User.FindFirstValue("sub")!);
        var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                         ?? httpContext.User.FindFirstValue("name")!;

        try
        {
            var result = await handler.Handle(new CreateGameCommand(playerName, playerId), ct);
            return Results.Ok(new CreateGameResponse(result.GameId, result.GameCode));
        }
        catch (DomainException ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status503ServiceUnavailable);
        }
    }

    private static async Task<IResult> GetGame(
        string code,
        HttpContext httpContext,
        IQueryHandler<GetGameQuery, GameDto?> handler,
        CancellationToken ct)
    {
        var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.User.FindFirstValue("sub")!);
        try
        {
            var result = await handler.Handle(new GetGameQuery(code, playerId), ct);
            return result is null ? Results.NotFound() : Results.Ok(result);
        }
        catch (ForbiddenException)
        {
            return Results.Forbid();
        }
    }

    private static async Task<IResult> GetGameEvents(
        string code,
        string? playerId,
        IQueryHandler<GetGameQuery, GameDto?> gameHandler,
        ISseGameEventService sseService,
        HttpContext httpContext,
        CancellationToken ct)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
            return Results.Unauthorized();

        GameDto? game;
        try
        {
            game = await gameHandler.Handle(new GetGameQuery(code, parsedPlayerId), ct);
        }
        catch (ForbiddenException)
        {
            return Results.Forbid();
        }

        if (game is null)
            return Results.NotFound();

        await sseService.StreamEventsAsync(code, parsedPlayerId, game, httpContext.Response, ct);
        return Results.Empty;
    }

    private static async Task<IResult> JoinGame(
        string code,
        HttpContext httpContext,
        ICommandHandler<JoinGameCommand, JoinGameResult> handler,
        CancellationToken ct)
    {
        var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.User.FindFirstValue("sub")!);
        var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                         ?? httpContext.User.FindFirstValue("name")!;

        try
        {
            var result = await handler.Handle(new JoinGameCommand(code, playerId, playerName), ct);
            return Results.Ok(new JoinGameResponse(result.GameId, result.PlayerId, result.GameCode));
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
        catch (DomainException ex)
        {
            return Results.Conflict(new { error = ex.Message });
        }
    }

    private static async Task<IResult> LeaveGame(
        string code,
        HttpContext httpContext,
        IPlayerPresenceTracker presenceTracker,
        CancellationToken ct)
    {
        var playerIdStr = httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? httpContext.User.FindFirstValue("sub");
        if (!Guid.TryParse(playerIdStr, out var playerId))
            return Results.Unauthorized();

        await presenceTracker.LeaveAsync(code, playerId, ct);
        return Results.NoContent();
    }
}
