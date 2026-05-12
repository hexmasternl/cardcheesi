using System.Collections.Concurrent;
using System.Threading.Channels;
using CardCheesi.Game.Abstractions;

namespace CardCheesi.Game;

public sealed class SseConnectionManager : ISseConnectionManager
{
    private readonly ConcurrentDictionary<string, List<Channel<SseEvent>>> _connections = new();
    private readonly Lock _lock = new();

    public void AddConnection(string gameCode, Channel<SseEvent> channel)
    {
        var list = _connections.GetOrAdd(gameCode, _ => []);
        lock (_lock)
        {
            list.Add(channel);
        }
    }

    public void RemoveConnection(string gameCode, Channel<SseEvent> channel)
    {
        if (!_connections.TryGetValue(gameCode, out var list)) return;
        lock (_lock)
        {
            list.Remove(channel);
        }
        channel.Writer.TryComplete();
    }

    public async Task BroadcastAsync(string gameCode, SseEvent sseEvent, CancellationToken ct = default)
    {
        if (!_connections.TryGetValue(gameCode, out var list)) return;

        List<Channel<SseEvent>> snapshot;
        lock (_lock)
        {
            snapshot = [..list];
        }

        foreach (var channel in snapshot)
        {
            await channel.Writer.WriteAsync(sseEvent, ct);
        }
    }
}
