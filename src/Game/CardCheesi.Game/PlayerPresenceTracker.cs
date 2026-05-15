using System.Collections.Concurrent;
using System.Text.Json;
using CardCheesi.Game.Abstractions;
using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game;

public sealed class PlayerPresenceTracker : IPlayerPresenceTracker
{
    private sealed record PlayerState(
        string PlayerName,
        PlayerPresenceStatus Status,
        CancellationTokenSource? GraceTimer);

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, PlayerState>> _games = new();
    private readonly ISseConnectionManager _connectionManager;

    private static readonly TimeSpan GracePeriod = TimeSpan.FromSeconds(30);

    public PlayerPresenceTracker(ISseConnectionManager connectionManager)
    {
        _connectionManager = connectionManager;
    }

    public IReadOnlyList<PlayerPresenceEvent> GetSnapshot(string gameCode)
    {
        if (!_games.TryGetValue(gameCode, out var players))
            return [];

        return players
            .Select(kv => new PlayerPresenceEvent(kv.Key, kv.Value.PlayerName, kv.Value.Status))
            .ToList();
    }

    public async Task ConnectAsync(string gameCode, Guid playerId, string playerName, CancellationToken ct = default)
    {
        var players = _games.GetOrAdd(gameCode, _ => new ConcurrentDictionary<Guid, PlayerState>());

        if (players.TryGetValue(playerId, out var existing))
        {
            // Cancel any pending grace timer
            existing.GraceTimer?.Cancel();
            existing.GraceTimer?.Dispose();
        }

        players[playerId] = new PlayerState(playerName, PlayerPresenceStatus.Connected, null);

        await BroadcastStatusAsync(gameCode, playerId, playerName, PlayerPresenceStatus.Connected, ct);
    }

    public async Task DisconnectAsync(string gameCode, Guid playerId, CancellationToken ct = default)
    {
        if (!_games.TryGetValue(gameCode, out var players)) return;
        if (!players.TryGetValue(playerId, out var existing)) return;

        // Do not downgrade a player who has explicitly left
        if (existing.Status == PlayerPresenceStatus.Left) return;

        var graceCts = new CancellationTokenSource();
        players[playerId] = existing with { Status = PlayerPresenceStatus.Disconnected, GraceTimer = graceCts };

        await BroadcastStatusAsync(gameCode, playerId, existing.PlayerName, PlayerPresenceStatus.Disconnected, ct);

        // Start grace period — promote to Left if player doesn't reconnect
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(GracePeriod, graceCts.Token);

                if (!players.TryGetValue(playerId, out var current)) return;
                if (current.Status != PlayerPresenceStatus.Disconnected) return;

                players[playerId] = current with { Status = PlayerPresenceStatus.Left, GraceTimer = null };
                await BroadcastStatusAsync(gameCode, playerId, current.PlayerName, PlayerPresenceStatus.Left);
            }
            catch (OperationCanceledException)
            {
                // Grace period was cancelled — player reconnected
            }
        }, CancellationToken.None);
    }

    public async Task LeaveAsync(string gameCode, Guid playerId, CancellationToken ct = default)
    {
        if (!_games.TryGetValue(gameCode, out var players)) return;
        if (!players.TryGetValue(playerId, out var existing)) return;

        // Idempotent — already left
        if (existing.Status == PlayerPresenceStatus.Left) return;

        // Cancel any pending grace timer
        existing.GraceTimer?.Cancel();
        existing.GraceTimer?.Dispose();

        players[playerId] = existing with { Status = PlayerPresenceStatus.Left, GraceTimer = null };
        await BroadcastStatusAsync(gameCode, playerId, existing.PlayerName, PlayerPresenceStatus.Left, ct);
    }

    private async Task BroadcastStatusAsync(
        string gameCode,
        Guid playerId,
        string playerName,
        PlayerPresenceStatus status,
        CancellationToken ct = default)
    {
        var payload = JsonSerializer.Serialize(new
        {
            playerId = playerId.ToString(),
            playerName,
            status = status.ToString(),
        });
        var sseEvent = new SseEvent("player-status", payload);
        await _connectionManager.BroadcastAsync(gameCode, sseEvent, ct);
    }
}
