using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Rules;

internal static class CardMoveEnumerator
{
    /// <summary>
    /// Returns all legal <see cref="MoveOption"/> values for <paramref name="playerId"/>
    /// when playing <paramref name="card"/>.
    /// </summary>
    public static IReadOnlyList<MoveOption> EnumerateMoves(GameState state, Guid playerId, Card card)
    {
        int playerIndex = MoveValidator.GetPlayerIndex(playerId, state.Players);
        if (playerIndex < 0) return [];

        var controlledPawns = GetControlledPawns(state, playerId, playerIndex);
        if (controlledPawns.Count == 0) return [];

        return card.Rank switch
        {
            CardRank.Ace   => EnumerateAceMoves(state, controlledPawns),
            CardRank.King  => EnumerateKingMoves(state, controlledPawns),
            CardRank.Four  => EnumerateFourMoves(state, controlledPawns),
            CardRank.Seven => EnumerateSevenMoves(state, controlledPawns),
            CardRank.Jack  => EnumerateJackMoves(state, playerId, playerIndex),
            CardRank.Queen => EnumerateForwardMoves(state, controlledPawns, 12),
            _              => EnumerateForwardMoves(state, controlledPawns, (int)card.Rank),
        };
    }

    // -----------------------------------------------------------------------
    // Proxy / control logic
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns the pawns this player controls.
    /// If all 4 own pawns are finished the player controls their teammate's pawns instead.
    /// </summary>
    private static IReadOnlyList<Pawn> GetControlledPawns(
        GameState state, Guid playerId, int playerIndex)
    {
        var ownPlayer = state.Players[playerIndex];
        bool allFinished = ownPlayer.Pawns.Count == 4 &&
                           ownPlayer.Pawns.All(p => p.Location is FinishLocation);

        if (allFinished)
        {
            int teammateIdx = MoveValidator.GetTeammateIndex(playerIndex);
            if (teammateIdx >= state.Players.Count) return [];
            return state.Players[teammateIdx].Pawns;
        }

        return ownPlayer.Pawns;
    }

    // -----------------------------------------------------------------------
    // Per-rank enumerators
    // -----------------------------------------------------------------------

    /// <summary>Ace: enter a reserve pawn to home OR advance an in-play board pawn +1.</summary>
    private static IReadOnlyList<MoveOption> EnumerateAceMoves(
        GameState state, IReadOnlyList<Pawn> controlled)
    {
        var moves = new List<MoveOption>();

        foreach (var pawn in controlled)
        {
            int pawnPlayerIndex = MoveValidator.GetPlayerIndex(pawn.OwnerId, state.Players);

            if (pawn.Location is ReserveLocation)
            {
                if (MoveValidator.CanEnterHome(pawnPlayerIndex, pawn.OwnerId, state))
                    moves.Add(new SingleMove(pawn.Id, 0));
            }
            else if (pawn.Location is BoardLocation bl)
            {
                var dest = MoveValidator.TryAdvanceFromBoard(
                    bl.Position, 1, pawn.OwnerId, pawnPlayerIndex, state);
                if (dest is not null)
                    moves.Add(new SingleMove(pawn.Id, 1));
            }
        }

        return moves;
    }

    /// <summary>King: enter a reserve pawn to home only.</summary>
    private static IReadOnlyList<MoveOption> EnumerateKingMoves(
        GameState state, IReadOnlyList<Pawn> controlled)
    {
        var moves = new List<MoveOption>();

        foreach (var pawn in controlled)
        {
            int pawnPlayerIndex = MoveValidator.GetPlayerIndex(pawn.OwnerId, state.Players);
            if (pawn.Location is ReserveLocation &&
                MoveValidator.CanEnterHome(pawnPlayerIndex, pawn.OwnerId, state))
            {
                moves.Add(new SingleMove(pawn.Id, 0));
            }
        }

        return moves;
    }

    /// <summary>Four: retreat an in-play board pawn 4 steps backward.</summary>
    private static IReadOnlyList<MoveOption> EnumerateFourMoves(
        GameState state, IReadOnlyList<Pawn> controlled)
    {
        var moves = new List<MoveOption>();

        foreach (var pawn in controlled)
        {
            if (pawn.Location is BoardLocation bl)
            {
                var dest = MoveValidator.TryRetreatFromBoard(bl.Position, 4, pawn.OwnerId, state);
                if (dest is not null)
                    moves.Add(new SingleMove(pawn.Id, -4));
            }
        }

        return moves;
    }

    /// <summary>
    /// Seven: move one pawn 7 forward, OR split 7 steps across two different board pawns.
    /// The second pawn's validity is checked against the original state (simplification).
    /// </summary>
    private static IReadOnlyList<MoveOption> EnumerateSevenMoves(
        GameState state, IReadOnlyList<Pawn> controlled)
    {
        var moves = new List<MoveOption>();
        var boardPawns = controlled.Where(p => p.Location is BoardLocation).ToList();

        // Single move of 7
        foreach (var pawn in boardPawns)
        {
            int pawnPlayerIndex = MoveValidator.GetPlayerIndex(pawn.OwnerId, state.Players);
            var bl = (BoardLocation)pawn.Location;
            var dest = MoveValidator.TryAdvanceFromBoard(bl.Position, 7, pawn.OwnerId, pawnPlayerIndex, state);
            if (dest is not null)
                moves.Add(new SingleMove(pawn.Id, 7));
        }

        // Splits: steps1 + steps2 = 7, two different pawns.
        // Pawn 2 is validated against the state AFTER step 1 has been applied so the
        // enumeration matches what MakeSplitMove will actually execute.
        for (int i = 0; i < boardPawns.Count; i++)
        {
            for (int j = 0; j < boardPawns.Count; j++)
            {
                if (i == j) continue;

                var pawn1 = boardPawns[i];
                var pawn2 = boardPawns[j];
                int pawnIdx1 = MoveValidator.GetPlayerIndex(pawn1.OwnerId, state.Players);
                var bl1 = (BoardLocation)pawn1.Location;

                for (int steps1 = 1; steps1 <= 6; steps1++)
                {
                    int steps2 = 7 - steps1;

                    var dest1 = MoveValidator.TryAdvanceFromBoard(
                        bl1.Position, steps1, pawn1.OwnerId, pawnIdx1, state);
                    if (dest1 is null) continue;

                    GameState afterStep1;
                    try
                    {
                        afterStep1 = state.MakeMove(pawn1.Id, steps1);
                    }
                    catch (InvalidOperationException)
                    {
                        continue;
                    }

                    // Re-locate pawn2 in the post-step1 state (it may have been hit & sent
                    // to reserve by step 1, or moved if pawn2 was the one being hit).
                    var pawn2After = MoveValidator.AllPawns(afterStep1)
                        .FirstOrDefault(p => p.Id == pawn2.Id);
                    if (pawn2After is null) continue;
                    if (pawn2After.Location is not BoardLocation bl2After) continue;

                    int pawnIdx2 = MoveValidator.GetPlayerIndex(pawn2.OwnerId, afterStep1.Players);

                    var dest2 = MoveValidator.TryAdvanceFromBoard(
                        bl2After.Position, steps2, pawn2.OwnerId, pawnIdx2, afterStep1);
                    if (dest2 is null) continue;

                    var split = new SplitMove(pawn1.Id, steps1, pawn2.Id, steps2);
                    var splitRev = new SplitMove(pawn2.Id, steps2, pawn1.Id, steps1);
                    if (!moves.Contains(split) && !moves.Contains(splitRev))
                        moves.Add(split);
                }
            }
        }

        return moves;
    }

    /// <summary>
    /// Jack: swap positions of two board pawns belonging to different owners.
    /// A protected pawn belonging to anyone other than the controller (including a teammate)
    /// cannot be swapped. Pawns in the finish corridor cannot be swapped.
    /// In proxy mode the "controller" pawns are the teammate's pawns, so the teammate's
    /// protected pawn IS the controller's pawn and may participate in a swap — this is
    /// naturally handled by <see cref="GetControlledPawns"/>.
    /// </summary>
    private static IReadOnlyList<MoveOption> EnumerateJackMoves(
        GameState state, Guid playerId, int playerIndex)
    {
        var moves = new List<MoveOption>();

        var controllerBoardPawns = GetControlledPawns(state, playerId, playerIndex)
            .Where(p => p.Location is BoardLocation)
            .ToList();

        var allBoardPawns = MoveValidator.AllPawns(state)
            .Where(p => p.Location is BoardLocation)
            .ToList();

        foreach (var myPawn in controllerBoardPawns)
        {
            foreach (var otherPawn in allBoardPawns)
            {
                if (otherPawn.OwnerId == myPawn.OwnerId) continue; // same owner

                // Cannot swap a protected pawn that the controller does not own.
                if (otherPawn.IsProtected) continue;

                var swap = new SwapMove(myPawn.Id, otherPawn.Id);
                var swapRev = new SwapMove(otherPawn.Id, myPawn.Id);
                if (!moves.Contains(swap) && !moves.Contains(swapRev))
                    moves.Add(swap);
            }
        }

        return moves;
    }

    /// <summary>Forward moves for numeric cards (2–10) and Queen (12).</summary>
    private static IReadOnlyList<MoveOption> EnumerateForwardMoves(
        GameState state, IReadOnlyList<Pawn> controlled, int steps)
    {
        var moves = new List<MoveOption>();

        foreach (var pawn in controlled)
        {
            int pawnPlayerIndex = MoveValidator.GetPlayerIndex(pawn.OwnerId, state.Players);
            if (pawn.Location is BoardLocation bl)
            {
                var dest = MoveValidator.TryAdvanceFromBoard(
                    bl.Position, steps, pawn.OwnerId, pawnPlayerIndex, state);
                if (dest is not null)
                    moves.Add(new SingleMove(pawn.Id, steps));
            }
        }

        return moves;
    }
}
