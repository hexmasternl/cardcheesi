using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.Rules;

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

    /// <summary>Adds a player to the game lobby. Returns a new <see cref="GameState"/> with the player included.</summary>
    public GameState AddPlayer(Player player)
        => this with { Players = Players.Append(player).ToList().AsReadOnly() };

    IGameState IGameState.AddPlayer(IPlayer player) => AddPlayer((Player)player);

    // -----------------------------------------------------------------------
    // PlayCard
    // -----------------------------------------------------------------------

    /// <summary>
    /// Removes one occurrence of <paramref name="card"/> from the player's hand.
    /// Does not apply any pawn movement.
    /// </summary>
    public GameState PlayCard(Guid playerId, Card card)
    {
        if (Hands is null)
            throw new InvalidOperationException("Game has no active hands.");

        var hand = Hands.FirstOrDefault(h => h.PlayerId == playerId)
            ?? throw new InvalidOperationException($"No hand found for player {playerId}.");

        var cardList = hand.Cards.ToList();
        var idx = cardList.FindIndex(c => c == card);
        if (idx < 0)
            throw new InvalidOperationException($"Card {card} is not in player {playerId}'s hand.");

        cardList.RemoveAt(idx);
        var updatedHand = new PlayerHand(playerId, cardList.AsReadOnly());
        var newHands = Hands
            .Select(h => h.PlayerId == playerId ? updatedHand : h)
            .ToList()
            .AsReadOnly();

        return this with { Hands = newHands };
    }

    IGameState IGameState.PlayCard(Guid playerId, Card card) => PlayCard(playerId, card);

    // -----------------------------------------------------------------------
    // MakeMove
    // -----------------------------------------------------------------------

    /// <summary>
    /// Moves the pawn.
    /// <list type="bullet">
    ///   <item><term>spaces == 0</term><description>Enter reserve pawn at home (Ace / King).</description></item>
    ///   <item><term>spaces &gt; 0</term><description>Advance forward.</description></item>
    ///   <item><term>spaces &lt; 0</term><description>Retreat backward (Four card = −4).</description></item>
    /// </list>
    /// </summary>
    public GameState MakeMove(Guid pawnId, int spaces)
    {
        // Locate the pawn
        Player? ownerPlayer = null;
        Pawn? pawn = null;
        int playerIndex = -1;

        for (int i = 0; i < Players.Count; i++)
        {
            var found = Players[i].Pawns.FirstOrDefault(pw => pw.Id == pawnId);
            if (found is not null)
            {
                ownerPlayer = Players[i];
                pawn = found;
                playerIndex = i;
                break;
            }
        }

        if (pawn is null || ownerPlayer is null)
            throw new InvalidOperationException($"Pawn {pawnId} not found.");

        // --- Enter home (spaces == 0) ---
        if (spaces == 0)
        {
            if (pawn.Location is not ReserveLocation)
                throw new InvalidOperationException("Pawn must be in reserve to enter home.");

            if (!MoveValidator.CanEnterHome(playerIndex, pawn.OwnerId, this))
                throw new InvalidOperationException("Home position is already occupied by an own pawn.");

            int homePos = BoardRules.HomePosition(playerIndex);
            var movedPawn = pawn with
            {
                Location = new BoardLocation(homePos),
                Status = PawnStatus.InPlay,
                IsProtected = true,
            };
            return UpdatePawn(this, ownerPlayer, movedPawn);
        }

        // --- Advance (spaces > 0) ---
        if (spaces > 0)
        {
            if (pawn.Location is not BoardLocation bl)
                throw new InvalidOperationException("Pawn must be on the board to advance.");

            var dest = MoveValidator.TryAdvanceFromBoard(bl.Position, spaces, pawn.OwnerId, playerIndex, this)
                ?? throw new InvalidOperationException(
                    $"Cannot advance pawn {pawnId} by {spaces} step(s): path blocked or overshoot.");

            bool enteringFinish = dest is FinishLocation;
            var movedPawn = pawn with
            {
                Location = dest,
                Status = enteringFinish ? PawnStatus.Finished : PawnStatus.InPlay,
                IsProtected = enteringFinish, // gains protection in finish; loses it on board
            };

            var state = UpdatePawn(this, ownerPlayer, movedPawn);

            // Hit detection: if landed on a board square that has an unprotected enemy pawn
            if (dest is BoardLocation destBl)
                state = ApplyHit(state, pawnId, pawn.OwnerId, destBl.Position);

            return state;
        }

        // --- Retreat (spaces < 0, Four card) ---
        {
            if (pawn.Location is not BoardLocation bl)
                throw new InvalidOperationException("Pawn must be on the board to retreat.");

            int stepsBack = Math.Abs(spaces);
            var dest = MoveValidator.TryRetreatFromBoard(bl.Position, stepsBack, pawn.OwnerId, this)
                ?? throw new InvalidOperationException(
                    $"Cannot retreat pawn {pawnId} by {stepsBack} step(s): path blocked.");

            var movedPawn = pawn with
            {
                Location = dest,
                Status = PawnStatus.InPlay,
                IsProtected = false,
            };

            var state = UpdatePawn(this, ownerPlayer, movedPawn);
            state = ApplyHit(state, pawnId, pawn.OwnerId, dest.Position);
            return state;
        }
    }

    IGameState IGameState.MakeMove(Guid pawnId, int spaces) => MakeMove(pawnId, spaces);

    // -----------------------------------------------------------------------
    // SwapPawns
    // -----------------------------------------------------------------------

    /// <summary>Swaps the board positions of two pawns belonging to different owners (Jack card).</summary>
    public GameState SwapPawns(Guid pawnId1, Guid pawnId2)
    {
        Pawn? pawn1 = null, pawn2 = null;
        Player? player1 = null, player2 = null;

        foreach (var player in Players)
        {
            foreach (var p in player.Pawns)
            {
                if (p.Id == pawnId1) { pawn1 = p; player1 = player; }
                if (p.Id == pawnId2) { pawn2 = p; player2 = player; }
            }
        }

        if (pawn1 is null || player1 is null)
            throw new InvalidOperationException($"Pawn {pawnId1} not found.");
        if (pawn2 is null || player2 is null)
            throw new InvalidOperationException($"Pawn {pawnId2} not found.");
        if (pawn1.OwnerId == pawn2.OwnerId)
            throw new InvalidOperationException("Cannot swap pawns belonging to the same owner.");
        if (pawn1.Location is FinishLocation || pawn2.Location is FinishLocation)
            throw new InvalidOperationException("Cannot swap a pawn that is in the finish area.");

        // Both pawns lose protection after the swap
        var updated1 = pawn1 with { Location = pawn2.Location, IsProtected = false };
        var updated2 = pawn2 with { Location = pawn1.Location, IsProtected = false };

        var state = UpdatePawn(this, player1, updated1);
        // Re-fetch player2 from updated state in case player1 == player2 (same owner guarded above)
        state = UpdatePawn(state, state.Players.First(p => p.Id == player2.Id), updated2);
        return state;
    }

    IGameState IGameState.SwapPawns(Guid pawnId1, Guid pawnId2) => SwapPawns(pawnId1, pawnId2);

    // -----------------------------------------------------------------------
    // MakeSplitMove
    // -----------------------------------------------------------------------

    /// <summary>
    /// Applies a split Seven move. <paramref name="spaces1"/> + <paramref name="spaces2"/> must
    /// equal 7. When <paramref name="pawnId2"/> is null the full 7 steps go to
    /// <paramref name="pawnId1"/>.
    /// </summary>
    public GameState MakeSplitMove(Guid pawnId1, int spaces1, Guid? pawnId2, int spaces2)
    {
        if (spaces1 + spaces2 != 7)
            throw new ArgumentException("Split move step counts must sum to 7.", nameof(spaces1));

        var state = MakeMove(pawnId1, spaces1);

        if (pawnId2.HasValue)
            state = state.MakeMove(pawnId2.Value, spaces2);

        return state;
    }

    IGameState IGameState.MakeSplitMove(Guid pawnId1, int spaces1, Guid? pawnId2, int spaces2)
        => MakeSplitMove(pawnId1, spaces1, pawnId2, spaces2);

    // -----------------------------------------------------------------------
    // GetValidMoves / HasPlayableCards
    // -----------------------------------------------------------------------

    /// <summary>Returns all legal move options for the given player and card.</summary>
    public IReadOnlyList<MoveOption> GetValidMoves(Guid playerId, Card card)
        => CardMoveEnumerator.EnumerateMoves(this, playerId, card);

    IReadOnlyList<MoveOption> IGameState.GetValidMoves(Guid playerId, Card card)
        => GetValidMoves(playerId, card);

    /// <summary>Returns true when the player has at least one valid move with any card in hand.</summary>
    public bool HasPlayableCards(Guid playerId)
    {
        if (Hands is null) return false;
        var hand = Hands.FirstOrDefault(h => h.PlayerId == playerId);
        if (hand is null) return false;

        return hand.Cards.Any(card => GetValidMoves(playerId, card).Count > 0);
    }

    bool IGameState.HasPlayableCards(Guid playerId) => HasPlayableCards(playerId);

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private static GameState UpdatePawn(GameState state, Player ownerPlayer, Pawn updatedPawn)
    {
        var newPawns = ownerPlayer.Pawns
            .Select(p => p.Id == updatedPawn.Id ? updatedPawn : p)
            .ToList()
            .AsReadOnly();

        var updatedPlayer = ownerPlayer with { Pawns = newPawns };

        var newPlayers = state.Players
            .Select(p => p.Id == ownerPlayer.Id ? updatedPlayer : p)
            .ToList()
            .AsReadOnly();

        return state with { Players = newPlayers };
    }

    /// <summary>
    /// If an unprotected enemy pawn occupies <paramref name="boardPos"/> (other than
    /// <paramref name="movingPawnId"/>) it is sent to reserve.
    /// </summary>
    private static GameState ApplyHit(
        GameState state, Guid movingPawnId, Guid movingOwner, int boardPos)
    {
        var hitPawn = MoveValidator.AllPawns(state).FirstOrDefault(p =>
            p.Id != movingPawnId &&
            p.Location is BoardLocation hbl && hbl.Position == boardPos &&
            p.OwnerId != movingOwner);

        if (hitPawn is null) return state;

        var hitPlayer = state.Players.First(pl => pl.Id == hitPawn.OwnerId);
        var sentToReserve = hitPawn with
        {
            Location = new ReserveLocation(),
            Status = PawnStatus.Reserve,
            IsProtected = false,
        };
        return UpdatePawn(state, hitPlayer, sentToReserve);
    }
}

