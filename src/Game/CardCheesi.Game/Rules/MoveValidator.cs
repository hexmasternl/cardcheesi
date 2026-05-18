using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Rules;

internal static class MoveValidator
{
    /// <summary>Returns all pawns across all players.</summary>
    public static IEnumerable<Pawn> AllPawns(GameState state)
        => state.Players.SelectMany(p => p.Pawns);

    /// <summary>Returns the 0-based player index for the given owner id, or -1 if not found.</summary>
    public static int GetPlayerIndex(Guid ownerId, IReadOnlyList<Player> players)
    {
        for (int i = 0; i < players.Count; i++)
            if (players[i].Id == ownerId)
                return i;
        return -1;
    }

    /// <summary>Teammate index: 0↔2, 1↔3.</summary>
    public static int GetTeammateIndex(int playerIndex) => (playerIndex + 2) % 4;

    /// <summary>
    /// Returns true when a protected pawn NOT owned by <paramref name="movingPawnOwnerId"/> sits
    /// at <paramref name="boardPos"/>. Protected pawns block passing and landing.
    /// </summary>
    public static bool IsProtectedBlocker(int boardPos, Guid movingPawnOwnerId, GameState state)
        => AllPawns(state).Any(p =>
            p.Location is BoardLocation bl && bl.Position == boardPos &&
            p.IsProtected && p.OwnerId != movingPawnOwnerId);

    /// <summary>
    /// Returns true when every intermediate board position (steps 1 … steps-1 ahead of
    /// <paramref name="from"/>) is free of protected-enemy blockers.
    /// </summary>
    public static bool IsForwardPathClear(int from, int steps, Guid movingPawnOwnerId, GameState state)
    {
        var path = BoardRules.ForwardPath(from, steps - 1); // intermediates only, not landing
        return path.All(pos => !IsProtectedBlocker(pos, movingPawnOwnerId, state));
    }

    /// <summary>
    /// Returns true when every intermediate board position (steps 1 … steps-1 behind
    /// <paramref name="from"/>) is free of protected-enemy blockers.
    /// </summary>
    public static bool IsBackwardPathClear(int from, int steps, Guid movingPawnOwnerId, GameState state)
    {
        var path = BoardRules.BackwardPath(from, steps - 1); // intermediates only, not landing
        return path.All(pos => !IsProtectedBlocker(pos, movingPawnOwnerId, state));
    }

    /// <summary>
    /// Returns true when the moving pawn is allowed to land on <paramref name="position"/>.
    /// Blocked by: own pawn, teammate pawn, or protected enemy pawn.
    /// Allowed on: empty square, or unprotected enemy (which will be hit).
    /// </summary>
    public static bool CanLandOnBoardPosition(int position, Guid movingPawnOwnerId, GameState state)
    {
        int ownerIdx = GetPlayerIndex(movingPawnOwnerId, state.Players);
        int teammateIdx = ownerIdx >= 0 ? GetTeammateIndex(ownerIdx) : -1;
        Guid? teammateId = (teammateIdx >= 0 && teammateIdx < state.Players.Count)
            ? state.Players[teammateIdx].Id
            : null;

        return AllPawns(state).All(p =>
        {
            if (p.Location is not BoardLocation bl || bl.Position != position) return true;
            if (p.OwnerId == movingPawnOwnerId || p.OwnerId == teammateId) return false;
            if (p.IsProtected) return false;
            return true; // can hit unprotected enemy
        });
    }

    /// <summary>
    /// Tries to advance a pawn from <paramref name="currentPosition"/> by <paramref name="steps"/>
    /// forward. Returns the resulting <see cref="PawnLocation"/> (BoardLocation or FinishLocation),
    /// or null when the move is illegal.
    /// </summary>
    public static PawnLocation? TryAdvanceFromBoard(
        int currentPosition, int steps, Guid ownerId, int playerIndex, GameState state)
    {
        int homePos = BoardRules.HomePosition(playerIndex);
        int pathDist = BoardRules.PathDistance(currentPosition, homePos);

        if (pathDist + steps >= 64)
        {
            // Pawn enters (or overshoots) the finish corridor
            int finishSlot = pathDist + steps - 63;
            if (finishSlot > 4) return null; // overshoot

            // Steps needed on the board to reach the finish threshold
            int stepsToThreshold = 63 - pathDist;

            // Check board intermediates up to (but not including) the threshold
            if (stepsToThreshold > 0 &&
                !IsForwardPathClear(currentPosition, stepsToThreshold, ownerId, state))
                return null;

            // Check the threshold position itself for a protected blocker
            if (stepsToThreshold > 0 &&
                IsProtectedBlocker(
                    BoardRules.AdvanceBoardPosition(currentPosition, stepsToThreshold),
                    ownerId, state))
                return null;

            // Check finish slots: no own pawn may occupy any slot 1 … finishSlot
            var occupiedFinishSlots = AllPawns(state)
                .Where(p => p.OwnerId == ownerId && p.Location is FinishLocation)
                .Select(p => ((FinishLocation)p.Location).Slot)
                .ToHashSet();

            for (int slot = 1; slot <= finishSlot; slot++)
                if (occupiedFinishSlots.Contains(slot)) return null;

            return new FinishLocation(finishSlot);
        }
        else
        {
            // Stay on the shared board
            if (!IsForwardPathClear(currentPosition, steps, ownerId, state)) return null;

            int dest = BoardRules.AdvanceBoardPosition(currentPosition, steps);
            if (!CanLandOnBoardPosition(dest, ownerId, state)) return null;

            return new BoardLocation(dest);
        }
    }

    /// <summary>
    /// Tries to retreat a pawn <paramref name="steps"/> positions backward from
    /// <paramref name="currentPosition"/>. Returns the new <see cref="BoardLocation"/>,
    /// or null when the move is illegal.
    /// </summary>
    public static BoardLocation? TryRetreatFromBoard(
        int currentPosition, int steps, Guid ownerId, GameState state)
    {
        if (!IsBackwardPathClear(currentPosition, steps, ownerId, state)) return null;

        int dest = BoardRules.RetreatBoardPosition(currentPosition, steps);
        if (!CanLandOnBoardPosition(dest, ownerId, state)) return null;

        return new BoardLocation(dest);
    }

    /// <summary>
    /// Returns true when the home position for <paramref name="playerIndex"/> is not already
    /// occupied by one of the owner's own pawns.
    /// </summary>
    public static bool CanEnterHome(int playerIndex, Guid ownerId, GameState state)
    {
        int homePos = BoardRules.HomePosition(playerIndex);
        return AllPawns(state).All(p =>
            p.Location is not BoardLocation bl ||
            bl.Position != homePos ||
            p.OwnerId != ownerId);
    }
}
