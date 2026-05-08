using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record Player(
    Guid Id,
    string Name,
    IReadOnlyList<Pawn> Pawns) : IPlayer
{
    IReadOnlyList<IPawn> IPlayer.Pawns => Pawns;
}
