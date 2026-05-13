using CardCheesi.Game.Abstractions.DataTransferObjects;
using Microsoft.AspNetCore.Http;

namespace CardCheesi.Game.Abstractions;

/// <summary>Streams Server-Sent Events for a game session to a connected client.</summary>
public interface ISseGameEventService
{
    Task StreamEventsAsync(
        string gameCode,
        Guid playerId,
        GameDto game,
        HttpResponse response,
        CancellationToken ct);
}
