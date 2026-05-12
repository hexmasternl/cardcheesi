namespace CardCheesi.Game.Abstractions;

public interface ISseConnectionManager
{
    void AddConnection(string gameCode, System.Threading.Channels.Channel<SseEvent> channel);
    void RemoveConnection(string gameCode, System.Threading.Channels.Channel<SseEvent> channel);
    Task BroadcastAsync(string gameCode, SseEvent sseEvent, CancellationToken ct = default);
}
