using CardCheesi.Game.Abstractions;

namespace CardCheesi.Game;

public static class GameFactory
{
    /// <summary>
    /// Creates a <see cref="GameState"/> in the <see cref="GameStatus.Waiting"/> state
    /// with a single participant (the creator). Other players join via the join endpoint.
    /// </summary>
    public static GameState CreateWaiting(string creatorName, string gameCode)
    {
        var playerId = Guid.NewGuid();
        var creator = new Player(
            Id: playerId,
            Name: creatorName,
            Pawns: CreatePawns(playerId));

        return new GameState(
            Id: Guid.NewGuid(),
            GameCode: gameCode,
            Status: GameStatus.Waiting,
            Teams: Array.Empty<Team>(),
            Players: new[] { creator },
            Turn: null,
            Deck: null,
            Hands: null);
    }

    /// <summary>
    /// Creates a valid initial <see cref="GameState"/> for exactly 4 players.
    /// </summary>
    /// <param name="playerNames">Exactly 4 player names (non-null, non-empty).</param>
    /// <param name="gameCode">6-character unique game code.</param>
    /// <param name="rng">Optional RNG; defaults to <see cref="SystemRandom.Instance"/>.</param>
    /// <exception cref="ArgumentException">Thrown when the number of player names is not exactly 4.</exception>
    public static GameState Create(IReadOnlyList<string> playerNames, string gameCode, IRandom? rng = null)
    {
        if (playerNames.Count != 4)
            throw new ArgumentException("Exactly 4 player names are required.", nameof(playerNames));

        rng ??= SystemRandom.Instance;

        var players = playerNames
            .Select(name =>
            {
                var playerId = Guid.NewGuid();
                return new Player(
                    Id: playerId,
                    Name: name,
                    Pawns: CreatePawns(playerId));
            })
            .ToList();

        var teams = new List<Team>
        {
            new(Guid.NewGuid(), [players[0], players[2]]),
            new(Guid.NewGuid(), [players[1], players[3]])
        };

        var deck = Deck.Standard().Shuffle(rng);

        var hands = players
            .Select(p => new PlayerHand(p.Id, []))
            .ToList<PlayerHand>();

        var turnState = new TurnState(
            ActivePlayerId: players[0].Id,
            DealerId: players[0].Id,
            RoundNumber: 1);

        return new GameState(
            Id: Guid.NewGuid(),
            GameCode: gameCode,
            Status: GameStatus.InProgress,
            Teams: teams.AsReadOnly(),
            Players: players.AsReadOnly(),
            Turn: turnState,
            Deck: deck,
            Hands: hands.AsReadOnly());
    }

    private static IReadOnlyList<Pawn> CreatePawns(Guid ownerId)
    {
        return Enumerable.Range(0, 4)
            .Select(_ => new Pawn(
                Id: Guid.NewGuid(),
                OwnerId: ownerId,
                Status: PawnStatus.Reserve,
                Location: new ReserveLocation()))
            .ToList()
            .AsReadOnly();
    }
}

