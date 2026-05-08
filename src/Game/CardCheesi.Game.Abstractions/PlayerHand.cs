namespace CardCheesi.Game.Abstractions;

public record PlayerHand(
    Guid PlayerId,
    IReadOnlyList<Card> Cards);
