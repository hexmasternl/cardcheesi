using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.Abstractions;

public interface IPlayerPresenceTracker
{
    IReadOnlyList<PlayerPresenceEvent> GetSnapshot(string gameCode);
    Task ConnectAsync(string gameCode, Guid playerId, string playerName, CancellationToken ct = default);
    Task DisconnectAsync(string gameCode, Guid playerId, CancellationToken ct = default);
}
