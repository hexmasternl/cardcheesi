namespace CardCheesi.Game.Abstractions.DomainModels;

public interface IGameState
{
    Guid Id { get; }
    string GameCode { get; }
    GameStatus Status { get; }
    IReadOnlyList<ITeam> Teams { get; }
    IReadOnlyList<IPlayer> Players { get; }
    ITurnState? Turn { get; }
    IDeck? Deck { get; }
    IReadOnlyList<IPlayerHand>? Hands { get; }

    /// <summary>Adds a player to the game lobby. Returns a new game state with the player added.</summary>
    IGameState AddPlayer(IPlayer player);

    /// <summary>Plays a card from the specified player's hand. Returns the resulting game state.</summary>
    IGameState PlayCard(Guid playerId, Card card);

    /// <summary>Moves a pawn the specified number of spaces. Returns the resulting game state.</summary>
    IGameState MakeMove(Guid pawnId, int spaces);

    /// <summary>Swaps the board positions of two pawns (Jack card). Returns the resulting game state.</summary>
    IGameState SwapPawns(Guid pawnId1, Guid pawnId2);

    /// <summary>
    /// Applies a split Seven move: spaces1 + spaces2 must equal 7.
    /// If pawnId2 is null the full 7 steps are applied to pawnId1.
    /// </summary>
    IGameState MakeSplitMove(Guid pawnId1, int spaces1, Guid? pawnId2, int spaces2);

    /// <summary>Returns true if the player has at least one valid move with any card currently in hand.</summary>
    bool HasPlayableCards(Guid playerId);

    /// <summary>
    /// Discards every card in <paramref name="playerId"/>'s hand at once. A player MAY only
    /// dispose when they have no playable cards (see <see cref="HasPlayableCards"/>);
    /// partial disposes are not permitted.
    /// </summary>
    IGameState DisposeHand(Guid playerId);

    /// <summary>
    /// Advances the turn to the next player. When all hands become empty, deals the next
    /// round (or starts a new dealer's turn) automatically.
    /// </summary>
    IGameState AdvanceTurn(IRandom? rng = null);

    /// <summary>Returns all legal move options for the given player and card.</summary>
    IReadOnlyList<MoveOption> GetValidMoves(Guid playerId, Card card);
}
