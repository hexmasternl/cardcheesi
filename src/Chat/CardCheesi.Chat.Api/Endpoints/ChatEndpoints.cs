using System.Security.Claims;
using System.Threading.Channels;
using CardCheesi.Chat.Features.Chat;
using CardCheesi.Core;
using CardCheesi.Game.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CardCheesi.Chat.Api.Endpoints;

public static class ChatEndpoints
{
    public static IEndpointRouteBuilder MapChatEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/chat").WithTags("Chat");

        group.MapPost("/{code}", SendChatMessage)
            .WithName("SendChatMessage")
            .RequireAuthorization()
            .Produces(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .WithOpenApi();

        group.MapGet("/{code}/events", GetChatEvents)
            .WithName("ChatEvents");

        return app;
    }

    private static async Task<IResult> SendChatMessage(
        string code,
        SendChatMessageRequest request,
        HttpContext httpContext,
        ICommandHandler<SendChatMessageCommand> handler,
        CancellationToken ct)
    {
        var playerId = Guid.Parse(httpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)
                                  ?? httpContext.User.FindFirstValue("sub")!);
        var playerName = httpContext.User.FindFirstValue(ClaimTypes.Name)
                         ?? httpContext.User.FindFirstValue("name")!;

        try
        {
            await handler.Handle(new SendChatMessageCommand(code, playerId, playerName, request.Text), ct);
            return Results.Ok();
        }
        catch (DomainException ex)
        {
            return Results.BadRequest(new { error = ex.Message });
        }
        catch (ForbiddenException)
        {
            return Results.Forbid();
        }
        catch (NotFoundException ex)
        {
            return Results.NotFound(new { error = ex.Message });
        }
    }

    private static async Task<IResult> GetChatEvents(
        string code,
        string? playerId,
        IGameRepository gameRepository,
        ISseConnectionManager connectionManager,
        HttpResponse response,
        CancellationToken ct)
    {
        if (!Guid.TryParse(playerId, out var parsedPlayerId))
            return Results.Unauthorized();

        var game = await gameRepository.GetByCodeAsync(code, ct);
        if (game is null)
            return Results.NotFound();

        if (game.Players.All(p => p.Id != parsedPlayerId))
            return Results.Forbid();

        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions { SingleReader = true });
        connectionManager.AddConnection(code, channel);

        var keepAliveTimer = new PeriodicTimer(TimeSpan.FromSeconds(15));
        _ = Task.Run(async () =>
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
                    await response.WriteAsync($"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n", ct);

                await response.Body.FlushAsync(ct);
            }
        }
        catch (OperationCanceledException) { /* client disconnected */ }
        finally
        {
            keepAliveTimer.Dispose();
            connectionManager.RemoveConnection(code, channel);
        }

        return Results.Empty;
    }

    private sealed record SendChatMessageRequest(string Text);
}
