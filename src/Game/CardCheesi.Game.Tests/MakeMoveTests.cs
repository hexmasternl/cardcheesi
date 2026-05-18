using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>Tests for <see cref="GameState.MakeMove"/> covering enter-home, advance, finish, and hit detection.</summary>
public class MakeMoveTests
{
    private const string Code = "MVTEST";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // Enter home (spaces == 0)
    // -----------------------------------------------------------------------

    [Fact]
    public void EnterHome_PlacesPawnAtHomePosition_WithProtection()
    {
        var state = CreateGame();
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 0);

        var result = state.Players[0].Pawns.Single(p => p.Id == pawn.Id);
        var loc    = Assert.IsType<BoardLocation>(result.Location);
        Assert.Equal(1, loc.Position);         // P1 home = board position 1
        Assert.Equal(PawnStatus.InPlay, result.Status);
        Assert.True(result.IsProtected);
    }

    [Fact]
    public void EnterHome_WhenOccupiedByOwnPawn_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        var pawn0 = state.Players[0].Pawns[0];
        var pawn1 = state.Players[0].Pawns[1];

        state = state.MakeMove(pawn0.Id, 0);          // pawn0 enters home at position 1

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn1.Id, 0));
    }

    [Fact]
    public void EnterHome_FromNonReserveLocation_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);        // already on board
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 0));
    }

    // -----------------------------------------------------------------------
    // Advance forward (spaces > 0)
    // -----------------------------------------------------------------------

    [Fact]
    public void Advance_MovesForwardBySteps()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 3);

        var result = state.Players[0].Pawns.Single(p => p.Id == pawn.Id);
        Assert.Equal(8, ((BoardLocation)result.Location).Position);
        Assert.Equal(PawnStatus.InPlay, result.Status);
        Assert.False(result.IsProtected);
    }

    [Fact]
    public void Advance_LeavingHome_RemovesProtection()
    {
        var state = CreateGame();
        var pawn  = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 0);                // enter home → protected
        Assert.True(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);

        state = state.MakeMove(pawn.Id, 2);                // advance → loses protection
        Assert.False(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).IsProtected);
    }

    [Fact]
    public void Advance_WrapAroundBoard()
    {
        // P2 (home=17). Pawn at 63, pathDist = (63-17+64)%64 = 46. Advance 4 → position 3. 46+4=50<64.
        var state = CreateGame();
        state = state.WithPawnAtBoard(1, 0, 63);
        var pawn = state.Players[1].Pawns[0];

        state = state.MakeMove(pawn.Id, 4);

        Assert.Equal(3, ((BoardLocation)state.Players[1].Pawns.Single(p => p.Id == pawn.Id).Location).Position);
    }

    [Fact]
    public void Advance_FromReserve_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        var pawn  = state.Players[0].Pawns[0]; // in reserve

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 3));
    }

    // -----------------------------------------------------------------------
    // Entering finish
    // -----------------------------------------------------------------------

    [Fact]
    public void Advance_IntoFinishSlot1_SetsFinishedStatusAndProtection()
    {
        // P1 home=1. Pawn at position 64 (pathDist=63). Advance 1 → FinishLocation(1).
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 64);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 1);

        var result = state.Players[0].Pawns.Single(p => p.Id == pawn.Id);
        var finish = Assert.IsType<FinishLocation>(result.Location);
        Assert.Equal(1, finish.Slot);
        Assert.Equal(PawnStatus.Finished, result.Status);
        Assert.True(result.IsProtected);
    }

    [Fact]
    public void Advance_IntoFinishSlot4_ReachesLastSlot()
    {
        // P1 pawn at position 60 (pathDist=59). Advance 8: 59+8=67>=64, finishSlot=67-63=4.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 60);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 8);

        var finish = Assert.IsType<FinishLocation>(state.Players[0].Pawns.Single(p => p.Id == pawn.Id).Location);
        Assert.Equal(4, finish.Slot);
    }

    [Fact]
    public void Advance_OvershoottFinish_ThrowsInvalidOperation()
    {
        // P1 pawn at position 64 (pathDist=63). Advance 5 → finishSlot=5 > 4 → overshoot.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 64);
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 5));
    }

    [Fact]
    public void Advance_FinishSlotOccupiedByOwnPawn_ThrowsInvalidOperation()
    {
        // Two P1 pawns: one already in FinishLocation(1), other at position 64. Advance 1 → slot 1 → blocked.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 64);
        state = state.WithPawnInFinish(0, 1, 1);       // pawn1 already in slot 1
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 1));
    }

    // -----------------------------------------------------------------------
    // Hit detection
    // -----------------------------------------------------------------------

    [Fact]
    public void Advance_HitsUnprotectedEnemy_SendsEnemyToReserve()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);   // P1 at 5
        state = state.WithPawnAtBoard(1, 0, 8);   // P2 at 8 (unprotected)
        var p1Pawn = state.Players[0].Pawns[0];
        var p2Pawn = state.Players[1].Pawns[0];

        state = state.MakeMove(p1Pawn.Id, 3);     // 5 → 8

        Assert.Equal(8, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == p1Pawn.Id).Location).Position);
        Assert.IsType<ReserveLocation>(state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).Location);
        Assert.Equal(PawnStatus.Reserve, state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).Status);
    }

    [Fact]
    public void Advance_CannotLandOnTeammatePawn_ThrowsInvalidOperation()
    {
        // P1 (player[0]) and P3 (player[2]) are teammates (Team A).
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(2, 0, 8);   // teammate pawn at 8
        var p1Pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(p1Pawn.Id, 3));
    }

    [Fact]
    public void Advance_CannotLandOnOwnPawn_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 8);   // own pawn at 8
        var pawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.MakeMove(pawn.Id, 3));
    }

    // -----------------------------------------------------------------------
    // Miscellaneous
    // -----------------------------------------------------------------------

    [Fact]
    public void MakeMove_PawnNotFound_ThrowsInvalidOperation()
    {
        Assert.Throws<InvalidOperationException>(() => CreateGame().MakeMove(Guid.NewGuid(), 1));
    }
}
