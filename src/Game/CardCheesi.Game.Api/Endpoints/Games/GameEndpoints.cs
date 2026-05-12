using System.Security.Claims;
using System.Text.Json;
using System.Threading.Channels;
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
            .Produces<GameDto>()
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapGet("/{code}/events", GetGameEvents)
            .WithName("GameEvents");

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
        IQueryHandler<GetGameQuery, GameDto?> handler,
        CancellationToken ct)
    {
        var result = await handler.Handle(new GetGameQuery(code), ct);
        return result is null ? Results.NotFound() : Results.Ok(result);
    }

    private static async Task<IResult> GetGameEvents(
        string code,
        string? playerId,
        IQueryHandler<GetGameQuery, GameDto?> gameHandler,
        ISseConnectionManager connectionManager,
        IPlayerPresenceTracker presenceTracker,
        HttpContext httpContext,
        CancellationToken ct)
    {
        var game = await gameHandler.Handle(new GetGameQuery(code), ct);
        if (game is null)
            return Results.NotFound();

        var response = httpContext.Response;
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions { SingleReader = true });
        connectionManager.AddConnection(code, channel);

        Guid? parsedPlayerId = Guid.TryParse(playerId, out var pid) ? pid : null;
        string? playerName = parsedPlayerId.HasValue
            ? game.Players.FirstOrDefault(p => p.Id == parsedPlayerId.Value)?.Name
            : null;

        if (parsedPlayerId.HasValue && playerName is not null)
            await presenceTracker.ConnectAsync(code, parsedPlayerId.Value, playerName, ct);

        foreach (var evt in presenceTracker.GetSnapshot(code))
        {
            var snapshotPayload = JsonSerializer.Serialize(new
            {
                playerId = evt.PlayerId.ToString(),
                playerName = evt.PlayerName,
                status = evt.Status.ToString(),
            });
            await WriteSseEventAsync(response, new SseEvent("player-status", snapshotPayload), ct);
        }

        var keepAliveTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        var keepAliveTask = Task.Run(async () =>
        {
            while (await keepAliveTimer.WaitForNextTickAsync(ct))
                await channel.Writer.WriteAsync(new SseEvent("keep-alive", ""), ct);
        }, ct);

        try
        {
            await foreach (var sseEvent in channel.Reader.ReadAllAsync(ct))
            {
                if (sseEvent.EventType == "keep-alive")
                    await response.WriteAsync(": keep-alive\n\n", ct);
                else
                    await WriteSseEventAsync(response, sseEvent, ct);

                await response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
            keepAliveTimer.Dispose();
            connectionManager.RemoveConnection(code, channel);

            if (parsedPlayerId.HasValue && playerName is not null)
                await presenceTracker.DisconnectAsync(code, parsedPlayerId.Value);
        }

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

    private static Task WriteSseEventAsync(HttpResponse response, SseEvent sseEvent, CancellationToken ct)
        => response.WriteAsync($"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n", ct);
}
