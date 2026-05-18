using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests.TestHelpers;

/// <summary>
/// Extension methods for building <see cref="GameState"/> instances with specific pawn
/// configurations in unit tests — bypasses game-rule validation intentionally.
/// </summary>
internal static class GameStateBuilder
{
    /// <summary>
    /// Directly places pawn[<paramref name="pawnIndex"/>] of player[<paramref name="playerIndex"/>]
    /// at <paramref name="boardPos"/> on the shared board.
    /// </summary>
    public static GameState WithPawnAtBoard(
        this GameState state, int playerIndex, int pawnIndex, int boardPos, bool isProtected = false)
    {
        var player = state.Players[playerIndex];
        var pawn   = player.Pawns[pawnIndex];

        var updated = pawn with
        {
            Location    = new BoardLocation(boardPos),
            Status      = PawnStatus.InPlay,
            IsProtected = isProtected,
        };

        return ReplacePawn(state, player, updated);
    }

    /// <summary>
    /// Directly places pawn[<paramref name="pawnIndex"/>] of player[<paramref name="playerIndex"/>]
    /// into the finish corridor at <paramref name="slot"/> (1–4).
    /// </summary>
    public static GameState WithPawnInFinish(
        this GameState state, int playerIndex, int pawnIndex, int slot)
    {
        var player = state.Players[playerIndex];
        var pawn   = player.Pawns[pawnIndex];

        var updated = pawn with
        {
            Location    = new FinishLocation(slot),
            Status      = PawnStatus.Finished,
            IsProtected = true,
        };

        return ReplacePawn(state, player, updated);
    }

    /// <summary>
    /// Replaces the entire hand of player[<paramref name="playerIndex"/>] with the supplied cards.
    /// </summary>
    public static GameState WithHand(
        this GameState state, int playerIndex, params Card[] cards)
    {
        if (state.Hands is null) return state;

        var playerId = state.Players[playerIndex].Id;
        var newHands = state.Hands
            .Select(h => h.PlayerId == playerId
                ? new PlayerHand(playerId, cards.ToList().AsReadOnly())
                : h)
            .ToList()
            .AsReadOnly();

        return state with { Hands = newHands };
    }

    // -----------------------------------------------------------------------

    private static GameState ReplacePawn(GameState state, Player owner, Pawn updated)
    {
        var newPawns = owner.Pawns
            .Select(p => p.Id == updated.Id ? updated : p)
            .ToList()
            .AsReadOnly();

        var updatedOwner = owner with { Pawns = newPawns };

        var newPlayers = state.Players
            .Select(p => p.Id == owner.Id ? updatedOwner : p)
            .ToList()
            .AsReadOnly();

        return state with { Players = newPlayers };
    }
}
