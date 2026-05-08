namespace CardCheesi.Game.Abstractions;

public record TurnState(
    Guid ActivePlayerId,
    Guid DealerId,
    int RoundNumber)
{
    /// <summary>Cards dealt per player this round: 5 in round 1, 4 in rounds 2 and 3.</summary>
    public int CardsThisRound => RoundNumber == 1 ? 5 : 4;
}
