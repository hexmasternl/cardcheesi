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
    public void GetValidMoves_SevenCard_NoPawnsOnBoard_ReturnsEmpty()
    {
        var state = CreateGame();
        // All pawns in reserve by default
        state = state.WithHand(0, SevenCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, SevenCard);

        Assert.Empty(moves);
    }
}
