using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>Tests for Four-card (retreat -4 steps) movement.</summary>
public class FourMoveTests
{
    private static readonly Card FourCard = new(CardSuit.Hearts, CardRank.Four);
    private const string Code = "FOUR01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // MakeMove with -4
    // -----------------------------------------------------------------------

    [Fact]
    public void Retreat4_MovesBackwardFourSteps()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, -4);

        var result = state.Players[0].Pawns.Single(p => p.Id == pawn.Id);
        Assert.Equal(6, ((BoardLocation)result.Location).Position);    // 10 - 4 = 6
        Assert.Equal(PawnStatus.InPlay, result.Status);
        Assert.False(result.IsProtected);
    }

    [Fact]
    public void Retreat4_FromProtectedHomePawn_WrapsBackwardsAroundHome()
    {
        // P1's home is position 1. A freshly-entered (still protected) pawn at home
        // MAY move backwards — the path wraps across home: 1 → 64 → 63 → 62 → 61.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 1, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, -4);

        var result = state.Players[0].Pawns.Single(p => p.Id == pawn.Id);
        Assert.Equal(61, ((BoardLocation)result.Location).Position);
        Assert.False(result.IsProtected); // loses protection on move
    }

    [Fact]
    public void Retreat4_UnprotectedPawnCrossingOwnHomeBackwards_ThrowsInvalidOperation()
    {
        // P1's home is position 1. An unprotected pawn at position 3 trying to retreat 4
        // would pass through home (path: 2, 1, 64, 63). Illegal once the pawn has moved.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 3);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, -4));
    }

    [Fact]
    public void Retreat4_UnprotectedPawnLandingExactlyOnOwnHome_IsAllowed()
    {
        // Landing on home backwards is not "crossing" — it stops there. Allowed.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, -4);

        Assert.Equal(1, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn.Id).Location).Position);
    }

    [Fact]
    public void Retreat4_LandsOnUnprotectedEnemy_SendsEnemyToReserve()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);   // P1 at 10
        state = state.WithPawnAtBoard(1, 0, 6);    // P2 at 6 (unprotected)
        var p1Pawn = state.Players[0].Pawns[0];
        var p2Pawn = state.Players[1].Pawns[0];

        state = state.MakeMove(p1Pawn.Id, -4);    // P1 retreats to 6, hits P2

        Assert.Equal(6, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == p1Pawn.Id).Location).Position);
        Assert.IsType<ReserveLocation>(state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).Location);
    }

    [Fact]
    public void Retreat4_BlockedByProtectedEnemyInPath_ThrowsInvalidOperation()
    {
        // P1 at 10. Protected P2 at 8. Backward path includes 9, 8, 7 (intermediates). Position 8 is blocked.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithPawnAtBoard(1, 0, 8, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, -4));
    }

    [Fact]
    public void Retreat4_LandingOnProtectedEnemy_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithPawnAtBoard(1, 0, 6, isProtected: true);  // protected P2 at landing position
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, -4));
    }

    [Fact]
    public void Retreat4_FromReserve_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        var pawn = state.Players[0].Pawns[0]; // in reserve

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, -4));
    }

    // -----------------------------------------------------------------------
    // GetValidMoves with Four card
    // -----------------------------------------------------------------------

    [Fact]
    public void GetValidMoves_FourCard_BoardPawn_ReturnsSingleMoveWithMinusFour()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;
        var pawnId   = state.Players[0].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        Assert.Single(moves);
        var sm = Assert.IsType<SingleMove>(moves[0]);
        Assert.Equal(pawnId, sm.PawnId);
        Assert.Equal(-4, sm.Steps);
    }

    [Fact]
    public void GetValidMoves_FourCard_AllPawnsInReserve_ReturnsEmpty()
    {
        var state = CreateGame();
        // All pawns start in reserve — no board pawn to retreat
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        Assert.Empty(moves);
    }

    [Fact]
    public void GetValidMoves_FourCard_PathBlockedByProtectedEnemy_ReturnsEmpty()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithPawnAtBoard(1, 0, 8, isProtected: true); // blocks intermediate
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        Assert.Empty(moves);
    }

    [Fact]
    public void GetValidMoves_FourCard_PathBlockedByOwnProtectedPawn_ExcludesThatMove()
    {
        // Protection blocks passing for EVERYONE — including the owner.
        // P1 moving pawn 0 at 10 backwards 4, with P1's OWN protected pawn at 8.
        // Pawn 0 must not be able to retreat (intermediate 8 is blocked).
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithPawnAtBoard(0, 1, 8, isProtected: true);
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;
        var blockedPawnId = state.Players[0].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        Assert.DoesNotContain(moves, m => m is SingleMove sm && sm.PawnId == blockedPawnId);
    }

    [Fact]
    public void MakeMove_FourCard_PathBlockedByOwnProtectedPawn_ThrowsInvalidOperation()
    {
        // Direct rule: even the owner of a protected pawn cannot pass it.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        state = state.WithPawnAtBoard(0, 1, 8, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, -4));
    }

    [Fact]
    public void GetValidMoves_FourCard_UnprotectedPawnNearHome_ReturnsEmpty()
    {
        // Can't cross own home backwards once moved.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 3); // P1 home is 1, pawn near home, not protected
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        Assert.Empty(moves);
    }

    [Fact]
    public void GetValidMoves_FourCard_ProtectedHomePawn_IsEnumerated()
    {
        // Freshly-entered protected pawn at home CAN retreat (lands close to its finish).
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 1, isProtected: true); // P1 at home, protected
        state = state.WithHand(0, FourCard);
        var playerId = state.Players[0].Id;
        var pawnId   = state.Players[0].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, FourCard);

        var sm = Assert.Single(moves);
        var single = Assert.IsType<SingleMove>(sm);
        Assert.Equal(pawnId, single.PawnId);
        Assert.Equal(-4, single.Steps);
    }

    // -----------------------------------------------------------------------
    // Forward "passing" is also blocked by protected pawns of any owner.
    // -----------------------------------------------------------------------

    [Fact]
    public void Forward_PathBlockedByOwnProtectedPawn_ThrowsInvalidOperation()
    {
        // P1 at 4 trying to move forward 5: intermediates 5, 6, 7, 8.
        // P1's OWN protected pawn at 6 blocks passing for everyone, including the owner.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 4);
        state = state.WithPawnAtBoard(0, 1, 6, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 5));
    }

    [Fact]
    public void Forward_PathBlockedByTeammateProtectedPawn_ThrowsInvalidOperation()
    {
        // P1 at 4, teammate P3's protected pawn at 6, P1 advances 5.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 4);
        state = state.WithPawnAtBoard(2, 0, 6, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 5));
    }
}
