namespace CardCheesi.Game.Abstractions;

public record Player(
    Guid Id,
    string Name,
    IReadOnlyList<Pawn> Pawns);
