namespace CardCheesi.Game.Abstractions.DomainModels;

public interface ITurnState
{
    Guid ActivePlayerId { get; }
    Guid DealerId { get; }
    int RoundNumber { get; }
    /// <summary>Cards dealt per player this round: 5 in round 1, 4 in rounds 2 and 3.</summary>
    int CardsThisRound { get; }
}
