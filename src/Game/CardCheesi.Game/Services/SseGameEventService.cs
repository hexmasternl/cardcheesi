using System.Text.Json;
using System.Threading.Channels;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DataTransferObjects;
using CardCheesi.Game.Abstractions.DomainModels;
using Microsoft.AspNetCore.Http;

namespace CardCheesi.Game.Services;

/// <summary>Manages SSE connections for a game, including presence tracking and keep-alive pings.</summary>
public sealed class SseGameEventService(
    ISseConnectionManager connectionManager,
    IPlayerPresenceTracker presenceTracker,
    IGameRepository gameRepository) : ISseGameEventService
{
    public async Task StreamEventsAsync(
        string gameCode,
        Guid playerId,
        GameDto game,
        HttpResponse response,
        CancellationToken ct)
    {
        response.Headers.ContentType = "text/event-stream";
        response.Headers.CacheControl = "no-cache";
        response.Headers.Append("X-Accel-Buffering", "no");
        response.Headers.Append("Connection", "keep-alive");

        var channel = Channel.CreateUnbounded<SseEvent>(new UnboundedChannelOptions { SingleReader = true });
        connectionManager.AddConnection(gameCode, channel);

        string? playerName = game.Players.FirstOrDefault(p => p.Id == playerId)?.Name;

        if (playerId != Guid.Empty && playerName is not null)
            await presenceTracker.ConnectAsync(gameCode, playerId, playerName, ct);

        foreach (var evt in presenceTracker.GetSnapshot(gameCode))
        {
            var snapshotPayload = JsonSerializer.Serialize(new
            {
                playerId = evt.PlayerId.ToString(),
                playerName = evt.PlayerName,
                status = evt.Status.ToString(),
            });
            await WriteSseEventAsync(response, new SseEvent("player-status", snapshotPayload), ct);
        }

        if (game.Status == GameStatus.InProgress && game.Turn?.ActivePlayerId == playerId)
        {
            var gameState = await gameRepository.GetByCodeAsync(gameCode, ct);
            bool canDispose = gameState is not null && !gameState.HasPlayableCards(playerId);
            var yourTurnPayload = JsonSerializer.Serialize(new
            {
                activePlayerId = playerId.ToString(),
                canDispose,
            });
            await WriteSseEventAsync(response, new SseEvent("your-turn", yourTurnPayload), ct);
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
            connectionManager.RemoveConnection(gameCode, channel);

            if (playerId != Guid.Empty && playerName is not null)
                await presenceTracker.DisconnectAsync(gameCode, playerId);
        }
    }

    private static Task WriteSseEventAsync(HttpResponse response, SseEvent sseEvent, CancellationToken ct)
        => response.WriteAsync($"event: {sseEvent.EventType}\ndata: {sseEvent.Data}\n\n", ct);
}

