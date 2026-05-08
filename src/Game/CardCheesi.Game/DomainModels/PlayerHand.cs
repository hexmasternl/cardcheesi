using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record PlayerHand(
    Guid PlayerId,
    IReadOnlyList<Card> Cards) : IPlayerHand;
