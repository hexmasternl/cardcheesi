namespace CardCheesi.Game.Abstractions.DomainModels;

public interface ITeam
{
    Guid Id { get; }
    IReadOnlyList<IPlayer> Players { get; }
}
