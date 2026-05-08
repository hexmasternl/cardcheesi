namespace CardCheesi.Game.Abstractions.DomainModels;

public interface IPlayer
{
    Guid Id { get; }
    string Name { get; }
    IReadOnlyList<IPawn> Pawns { get; }
}
