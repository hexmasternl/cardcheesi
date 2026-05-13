namespace CardCheesi.IntegrationMessages;

public sealed record PlayerCreatedEvent(
    Guid PlayerId,
    string PlayerName,
    Guid EventId,
    DateTimeOffset OccurredAt);
