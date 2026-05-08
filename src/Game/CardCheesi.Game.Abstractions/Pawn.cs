namespace CardCheesi.Game.Abstractions;

public record Pawn(
    Guid Id,
    Guid OwnerId,
    PawnStatus Status,
    PawnLocation Location);
