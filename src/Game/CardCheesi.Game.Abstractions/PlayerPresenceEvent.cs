namespace CardCheesi.Game.Abstractions.DomainModels;

public record PlayerPresenceEvent(
    Guid PlayerId,
    string PlayerName,
    PlayerPresenceStatus Status);
