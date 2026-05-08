namespace CardCheesi.Game.Abstractions.DomainModels;

public interface IPlayerHand
{
    Guid PlayerId { get; }
    IReadOnlyList<Card> Cards { get; }
}
