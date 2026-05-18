namespace CardCheesi.Game.Abstractions.DataTransferObjects;

/// <summary>
/// HTTP request body for the POST /games/{code}/move endpoint.
/// All information required to identify the card played and the move to perform.
/// </summary>
/// <param name="CardSuit">0=Clubs, 1=Diamonds, 2=Hearts, 3=Spades</param>
/// <param name="CardRank">1=Ace … 13=King</param>
/// <param name="PawnId">Primary pawn to act on (always required except for cardless moves).</param>
/// <param name="PawnId2">
/// Second pawn: required for Jack (swap target) and Seven split (second pawn).
/// Null for single-pawn moves.
/// </param>
/// <param name="Steps">
/// Steps to take. Required for all moves except Jack.
/// Examples: Ace enter = 0, Ace advance = 1, Four = -4, Seven full = 7,
/// Seven split = steps for PawnId (PawnId2 gets 7-Steps).
/// </param>
public sealed record MakeMoveRequest(
    int CardSuit,
    int CardRank,
    Guid PawnId,
    Guid? PawnId2,
    int? Steps);
