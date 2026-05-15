namespace CardCheesi.Game.Abstractions.DomainModels;

public sealed record ChatMessage(
    string GameCode,
    Guid SenderId,
    string SenderName,
    string Text,
    DateTimeOffset Timestamp);
