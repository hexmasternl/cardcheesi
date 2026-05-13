namespace CardCheesi.IntegrationMessages;

public sealed record PlayerLeftGameEvent(
    Guid PlayerId,
    string PlayerName,
    string GameCode,
    Guid EventId,
    DateTimeOffset OccurredAt);
