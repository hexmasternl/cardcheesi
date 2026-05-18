using Bogus;
using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests.Factories;

internal static class GameStateFactory
{
    private static readonly Faker _faker = new();

    public static GameState Create(
        Guid? id = null,
        string? gameCode = null,
        GameStatus? status = null,
        List<Player>? players = null,
        List<Team>? teams = null,
        TurnState? turn = null,
        IReadOnlyList<PlayerHand>? hands = null)
    {
        return new GameState(
            Id: id ?? Guid.NewGuid(),
            GameCode: gameCode ?? _faker.Random.AlphaNumeric(6).ToUpperInvariant(),
            Status: status ?? GameStatus.Waiting,
            Teams: teams ?? [],
            Players: players ?? [],
            Turn: turn,
            Deck: null,
            Hands: hands);
    }

    /// <summary>
    /// Creates a game state that is in-progress with a single player whose turn it is,
    /// holding the supplied hand and having one pawn at the given board position.
    /// </summary>
    public static (GameState Game, Player Player, Pawn Pawn) CreateInProgress(
        Guid? playerId = null,
        int boardPosition = 5,
        List<Card>? cards = null)
    {
        var pid = playerId ?? Guid.NewGuid();
        var pawnId = Guid.NewGuid();
        var pawn = new Pawn(pawnId, pid, PawnStatus.InPlay, new BoardLocation(boardPosition), false);
        var player = new Player(pid, _faker.Internet.UserName(), [pawn]);
        var hand = new PlayerHand(pid, (cards ?? [new Card(CardSuit.Clubs, CardRank.Two)]).AsReadOnly());
        var turn = new TurnState(pid, pid, 1);
        var game = Create(
            status: GameStatus.InProgress,
            players: [player],
            turn: turn,
            hands: [hand]);
        return (game, player, pawn);
    }
}
