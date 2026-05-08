using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record GameState(
    Guid Id,
    string GameCode,
    GameStatus Status,
    IReadOnlyList<Team> Teams,
    IReadOnlyList<Player> Players,
    TurnState? Turn,
    Deck? Deck,
    IReadOnlyList<PlayerHand>? Hands) : IGameState
{
    IReadOnlyList<ITeam> IGameState.Teams => Teams;
    IReadOnlyList<IPlayer> IGameState.Players => Players;
    ITurnState? IGameState.Turn => Turn;
    IDeck? IGameState.Deck => Deck;
    IReadOnlyList<IPlayerHand>? IGameState.Hands => Hands;

    /// <summary>
    /// Adds a player to the game lobby. Returns a new <see cref="GameState"/> with the player included.
    /// </summary>
    public GameState AddPlayer(Player player)
        => this with { Players = Players.Append(player).ToList().AsReadOnly() };

    IGameState IGameState.AddPlayer(IPlayer player) => AddPlayer((Player)player);

    /// <summary>
    /// Plays a card from the specified player's hand. Returns the resulting game state.
    /// </summary>
    public GameState PlayCard(Guid playerId, Card card)
        => throw new NotImplementedException("Card play logic is not yet implemented.");

    IGameState IGameState.PlayCard(Guid playerId, Card card) => PlayCard(playerId, card);

    /// <summary>
    /// Moves a pawn the specified number of spaces. Returns the resulting game state.
    /// </summary>
    public GameState MakeMove(Guid pawnId, int spaces)
        => throw new NotImplementedException("Move logic is not yet implemented.");

    IGameState IGameState.MakeMove(Guid pawnId, int spaces) => MakeMove(pawnId, spaces);
}
