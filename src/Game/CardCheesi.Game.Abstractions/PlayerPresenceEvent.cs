namespace CardCheesi.Game.Abstractions.DomainModels;

public sealed record PlayerPresenceEvent(
    Guid PlayerId,
    string PlayerName,
    PlayerPresenceStatus Status);
