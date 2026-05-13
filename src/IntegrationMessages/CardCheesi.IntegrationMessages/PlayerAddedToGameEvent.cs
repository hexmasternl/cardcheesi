namespace CardCheesi.IntegrationMessages;

public sealed record PlayerAddedToGameEvent(
    Guid GameId,
    string GameCode,
    Guid PlayerId,
    string PlayerName,
    Guid EventId,
    DateTimeOffset OccurredAt);
