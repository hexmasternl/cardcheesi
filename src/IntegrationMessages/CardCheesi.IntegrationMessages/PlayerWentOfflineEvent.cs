namespace CardCheesi.IntegrationMessages;

public sealed record PlayerWentOfflineEvent(
    Guid PlayerId,
    string PlayerName,
    string GameCode,
    Guid EventId,
    DateTimeOffset OccurredAt);
