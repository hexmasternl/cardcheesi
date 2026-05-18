using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>Tests for Seven-card movement: single move of 7 and split moves (steps1 + steps2 == 7).</summary>
public class SevenMoveTests
{
    private static readonly Card SevenCard = new(CardSuit.Hearts, CardRank.Seven);
    private const string Code = "SEVEN1";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // Single move of 7 via MakeMove
    // -----------------------------------------------------------------------

    [Fact]
    public void Move7_SinglePawn_AdvancesSeven()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var pawn = state.Players[0].Pawns[0];

        state = state.MakeMove(pawn.Id, 7);

        Assert.Equal(12, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn.Id).Location).Position);
    }

    // -----------------------------------------------------------------------
    // MakeSplitMove
    // -----------------------------------------------------------------------

    [Fact]
    public void MakeSplitMove_ThreeAndFour_MovesBothPawnsCorrectly()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);   // pawn0 at 5
        state = state.WithPawnAtBoard(0, 1, 10);  // pawn1 at 10
        var pawn0 = state.Players[0].Pawns[0];
        var pawn1 = state.Players[0].Pawns[1];

        state = state.MakeSplitMove(pawn0.Id, 3, pawn1.Id, 4);

        Assert.Equal(8,  ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn0.Id).Location).Position);
        Assert.Equal(14, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn1.Id).Location).Position);
    }

    [Fact]
    public void MakeSplitMove_OneAndSix_MovesBothPawnsCorrectly()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 20);
        var pawn0 = state.Players[0].Pawns[0];
        var pawn1 = state.Players[0].Pawns[1];

        state = state.MakeSplitMove(pawn0.Id, 1, pawn1.Id, 6);

        Assert.Equal(6,  ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn0.Id).Location).Position);
        Assert.Equal(26, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn1.Id).Location).Position);
    }

    [Fact]
    public void MakeSplitMove_StepsNotSumToSeven_ThrowsArgumentException()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 10);
        var pawn0 = state.Players[0].Pawns[0];
        var pawn1 = state.Players[0].Pawns[1];

        Assert.Throws<ArgumentException>(() => state.MakeSplitMove(pawn0.Id, 3, pawn1.Id, 5));
    }

    [Fact]
    public void MakeSplitMove_NullSecondPawn_AppliesFullSevenToFirst()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var pawn0 = state.Players[0].Pawns[0];

        state = state.MakeSplitMove(pawn0.Id, 7, null, 0);

        Assert.Equal(12, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == pawn0.Id).Location).Position);
    }

    // -----------------------------------------------------------------------
    // GetValidMoves with Seven card
    // -----------------------------------------------------------------------

    [Fact]
    public void GetValidMoves_SevenCard_SingleBoardPawn_ReturnsSingleSevenMove()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithHand(0, SevenCard);
        var playerId = state.Players[0].Id;
        var pawnId   = state.Players[0].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, SevenCard);

        Assert.Contains(moves, m => m is SingleMove sm && sm.PawnId == pawnId && sm.Steps == 7);
    }

    [Fact]
    public void GetValidMoves_SevenCard_TwoBoardPawns_IncludesSplitMoves()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 20);
        state = state.WithHand(0, SevenCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, SevenCard);

        Assert.Contains(moves, m => m is SplitMove);
    }

    [Fact]
    public void GetValidMoves_SevenCard_SplitDoesNotIncludeMoveBlockedByStep1Outcome()
    {
        // Setup: own pawn0 at 5, enemy pawn at 8 (unprotected).
        // Own pawn1 at 30.
        // A split of pawn0=3 + pawn1=4 would: step1 hits enemy at 8 (legal). step2 advances
        // pawn1 from 30 to 34 (legal in both old and new logic).
        // The enumerator must validate step2 against the POST-step1 state — this test makes
        // sure the change does not regress legitimate splits.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 8, isProtected: false);
        state = state.WithPawnAtBoard(0, 1, 30);
        state = state.WithHand(0, SevenCard);
        var playerId = state.Players[0].Id;
        var pawn0Id  = state.Players[0].Pawns[0].Id;
        var pawn1Id  = state.Players[0].Pawns[1].Id;

        var moves = state.GetValidMoves(playerId, SevenCard);

        Assert.Contains(moves, m =>
            m is SplitMove sp &&
            ((sp.PawnId1 == pawn0Id && sp.Steps1 == 3 && sp.PawnId2 == pawn1Id && sp.Steps2 == 4) ||
             (sp.PawnId1 == pawn1Id && sp.Steps1 == 4 && sp.PawnId2 == pawn0Id && sp.Steps2 == 3)));
    }

    [Fact]
    public void GetValidMoves_SevenCard_SplitRejectedWhenStep1MakesStep2Illegal()
    {
        // Own pawn0 at 5. Own pawn1 at 8.
        // Order A: pawn0 first, 3 steps → would land on own pawn1 at 8 (ILLEGAL).
        // Order B: pawn1 first, 4 steps → pawn1 vacates 8; then pawn0 advances 3 → 8 (LEGAL).
        // The post-step1-aware enumerator must reject A and accept B for this 3+4 distribution.
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 8);
        state = state.WithHand(0, SevenCard);
        var playerId = state.Players[0].Id;
        var pawn0Id  = state.Players[0].Pawns[0].Id;
        var pawn1Id  = state.Players[0].Pawns[1].Id;

        var moves = state.GetValidMoves(playerId, SevenCard);

        // The ILLEGAL ordering (pawn0 first by 3) must not be present.
        Assert.DoesNotContain(moves, m =>
            m is SplitMove sp && sp.PawnId1 == pawn0Id && sp.Steps1 == 3 && sp.PawnId2 == pawn1Id);

        // The LEGAL reverse ordering (pawn1 first by 4, then pawn0 by 3) should be enumerated.
        Assert.Contains(moves, m =>
            m is SplitMove sp && sp.PawnId1 == pawn1Id && sp.Steps1 == 4 && sp.PawnId2 == pawn0Id && sp.Steps2 == 3);

        // Sanity: a legal 1+6 split (pawn0→6, pawn1→14) should still be present.
        Assert.Contains(moves, m =>
            m is SplitMove sp &&
            ((sp.PawnId1 == pawn0Id && sp.Steps1 == 1 && sp.PawnId2 == pawn1Id && sp.Steps2 == 6) ||
             (sp.PawnId1 == pawn1Id && sp.Steps1 == 6 && sp.PawnId2 == pawn0Id && sp.Steps2 == 1)));
    }
}
