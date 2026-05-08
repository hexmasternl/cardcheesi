using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record Team(
    Guid Id,
    IReadOnlyList<Player> Players) : ITeam
{
    IReadOnlyList<IPlayer> ITeam.Players => Players;
}
