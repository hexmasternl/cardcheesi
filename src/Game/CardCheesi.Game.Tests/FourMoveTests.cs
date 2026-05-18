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
    public void Retreat4_WrapsAroundBoardBackward()
    {
        // 3 - 4 = -1 → wraps to 63
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 3);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, -4);

        Assert.Equal(63, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn.Id).Location).Position);
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
}
