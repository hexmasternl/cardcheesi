using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>
/// Tests that verify pawn protection rules:
/// gaining protection at home / finish, losing it on move, and blocking enemy movement.
/// </summary>
public class ProtectionTests
{
    private const string Code = "PROT01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // Gaining protection
    // -----------------------------------------------------------------------

    [Fact]
    public void EnteringHome_PawnIsProtected()
    {
        var state = CreateGame();
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 0);

        Assert.True(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);
    }

    [Fact]
    public void EnteringFinish_PawnIsProtected()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 64);   // at P1 finish threshold
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 1);        // enter finish slot 1

        Assert.True(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);
    }

    // -----------------------------------------------------------------------
    // Losing protection
    // -----------------------------------------------------------------------

    [Fact]
    public void MovingFromHome_PawnLosesProtection()
    {
        var state = CreateGame();
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 0);        // enter home → protected
        state = state.MakeMove(pawn.Id, 1);        // advance one step → loses protection

        Assert.False(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);
    }

    [Fact]
    public void Retreat_PawnLosesProtection()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10, isProtected: true);
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, -4);

        Assert.False(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);
    }

    // -----------------------------------------------------------------------
    // Protection blocks enemy movement
    // -----------------------------------------------------------------------

    [Fact]
    public void EnemyCannotPassThroughProtectedPawn()
    {
        // P1 at 5. Protected P2 at 7. P1 tries to advance 4 (path: 6,7,8 — 7 is intermediate → blocked).
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 7, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 4));
    }

    [Fact]
    public void EnemyCannotLandOnProtectedPawn()
    {
        // P1 at 5. Protected P2 at 8. P1 advances 3 → landing on 8 → blocked.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 8, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 3));
    }

    [Fact]
    public void EnemyCanLandOnUnprotectedPawn_AndHitsIt()
    {
        // P1 at 5. Unprotected P2 at 8. P1 advances 3 → hits P2.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 8, isProtected: false);
        var p1Pawn = state.Players[0].Pawns[0];
        var p2Pawn = state.Players[1].Pawns[0];

        state = state.MakeMove(p1Pawn.Id, 3);

        Assert.IsType<ReserveLocation>(state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).Location);
    }

    // -----------------------------------------------------------------------
    // Owner can move their own protected pawn
    // -----------------------------------------------------------------------

    [Fact]
    public void OwnerCanMoveOwnProtectedPawn()
    {
        // Protected own pawn at 5 — owner should be able to advance it.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5, isProtected: true);
        var pawn = state.Players[0].Pawns[0];

        // Must not throw
        state = state.MakeMove(pawn.Id, 3);

        Assert.Equal(8, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn.Id).Location).Position);
        Assert.False(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected); // loses protection
    }

    [Fact]
    public void ProtectedHomeAtHome_CannotBeHitByEnemy()
    {
        // P1 pawn at home (position 1, protected). P2 tries to advance 1 step from position 64 → position 1.
        // P2 path wraps. P2 home=17, pawn at 64, pathDist=(64-17+64)%64=47. Advance 1: stays on board at pos 1.
        // Cannot land at 1 because P1 is protected there.
        var state = CreateGame();
        state = state.MakeMove(state.Players[0].Pawns[0].Id, 0);  // P1 pawn enters home at 1, protected
        state = state.WithPawnAtBoard(1, 0, 64);                   // P2 pawn at 64
        var p2Pawn = state.Players[1].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(p2Pawn.Id, 1));
    }
}
