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
}
