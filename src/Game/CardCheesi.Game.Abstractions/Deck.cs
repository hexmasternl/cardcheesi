namespace CardCheesi.Game.Abstractions.DomainModels;

public interface IDeck
{
    IReadOnlyList<Card> Cards { get; }
    IDeck Shuffle(IRandom rng);
}
