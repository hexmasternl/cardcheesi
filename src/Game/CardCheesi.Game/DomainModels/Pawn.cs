using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record Pawn(
    Guid Id,
    Guid OwnerId,
    PawnStatus Status,
    PawnLocation Location,
    bool IsProtected = false) : IPawn;
