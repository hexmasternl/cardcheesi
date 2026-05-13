namespace CardCheesi.IntegrationMessages;

public sealed record PlayerCameOnlineEvent(
    Guid PlayerId,
    string PlayerName,
    string GameCode,
    Guid EventId,
    DateTimeOffset OccurredAt);
