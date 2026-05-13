namespace CardCheesi.IntegrationMessages;

public sealed record GameCreatedEvent(
    Guid GameId,
    string GameCode,
    Guid CreatorPlayerId,
    string CreatorPlayerName,
    Guid EventId,
    DateTimeOffset OccurredAt);
