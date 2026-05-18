using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>Tests for Jack-card swap mechanics.</summary>
public class JackMoveTests
{
    private static readonly Card JackCard = new(CardSuit.Hearts, CardRank.Jack);
    private const string Code = "JACK01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // SwapPawns
    // -----------------------------------------------------------------------

    [Fact]
    public void SwapPawns_ExchangesBoardPositions()
    {
        var state  = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 20);
        var p1Pawn = state.Players[0].Pawns[0];
        var p2Pawn = state.Players[1].Pawns[0];

        state = state.SwapPawns(p1Pawn.Id, p2Pawn.Id);

        Assert.Equal(20, ((BoardLocation)state.Players[0].Pawns.Single(p => p.Id == p1Pawn.Id).Location).Position);
        Assert.Equal(5,  ((BoardLocation)state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).Location).Position);
    }

    [Fact]
    public void SwapPawns_BothPawnsLoseProtection()
    {
        var state  = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5,  isProtected: true);
        state = state.WithPawnAtBoard(1, 0, 20, isProtected: false);
        var p1Pawn = state.Players[0].Pawns[0];
        var p2Pawn = state.Players[1].Pawns[0];

        state = state.SwapPawns(p1Pawn.Id, p2Pawn.Id);

        Assert.False(state.Players[0].Pawns.Single(p => p.Id == p1Pawn.Id).IsProtected);
        Assert.False(state.Players[1].Pawns.Single(p => p.Id == p2Pawn.Id).IsProtected);
    }

    [Fact]
    public void SwapPawns_SameOwner_ThrowsInvalidOperation()
    {
        var state  = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(0, 1, 20);
        var pawn0 = state.Players[0].Pawns[0];
        var pawn1 = state.Players[0].Pawns[1];

        Assert.Throws<InvalidOperationException>(() => state.SwapPawns(pawn0.Id, pawn1.Id));
    }

    [Fact]
    public void SwapPawns_FirstPawnInFinish_ThrowsInvalidOperation()
    {
        var state  = CreateGame();
        state = state.WithPawnInFinish(0, 0, 1);
        state = state.WithPawnAtBoard(1, 0, 20);
        var finishPawn = state.Players[0].Pawns[0];
        var boardPawn  = state.Players[1].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.SwapPawns(finishPawn.Id, boardPawn.Id));
    }

    [Fact]
    public void SwapPawns_SecondPawnInFinish_ThrowsInvalidOperation()
    {
        var state  = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnInFinish(1, 0, 1);
        var boardPawn  = state.Players[0].Pawns[0];
        var finishPawn = state.Players[1].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.SwapPawns(boardPawn.Id, finishPawn.Id));
    }

    [Fact]
    public void SwapPawns_PawnNotFound_ThrowsInvalidOperation()
    {
        var state = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var realPawn = state.Players[0].Pawns[0];

        Assert.Throws<InvalidOperationException>(() => state.SwapPawns(realPawn.Id, Guid.NewGuid()));
    }

    // -----------------------------------------------------------------------
    // GetValidMoves with Jack card
    // -----------------------------------------------------------------------

    [Fact]
    public void GetValidMoves_JackCard_ReturnSwapMoveWithEnemyBoardPawn()
    {
        var state   = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 20);
        state = state.WithHand(0, JackCard);
        var playerId = state.Players[0].Id;
        var p1Id     = state.Players[0].Pawns[0].Id;
        var p2Id     = state.Players[1].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, JackCard);

        Assert.Contains(moves, m =>
            m is SwapMove sw &&
            ((sw.PawnId1 == p1Id && sw.PawnId2 == p2Id) ||
             (sw.PawnId1 == p2Id && sw.PawnId2 == p1Id)));
    }

    [Fact]
    public void GetValidMoves_JackCard_ProtectedEnemyExcludedFromSwap()
    {
        var state   = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        state = state.WithPawnAtBoard(1, 0, 20, isProtected: true);  // enemy protected
        state = state.WithHand(0, JackCard);
        var playerId = state.Players[0].Id;
        var p2Id     = state.Players[1].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, JackCard);

        // Protected non-teammate pawn must NOT appear in any swap
        Assert.DoesNotContain(moves, m =>
            m is SwapMove sw && (sw.PawnId1 == p2Id || sw.PawnId2 == p2Id));
    }

    [Fact]
    public void GetValidMoves_JackCard_NoBoardPawns_ReturnsEmpty()
    {
        var state   = CreateGame();
        // All pawns in reserve — nothing to swap
        state = state.WithHand(0, JackCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, JackCard);

        Assert.Empty(moves);
    }

    [Fact]
    public void GetValidMoves_JackCard_OnlyOwnBoardPawnsNoEnemy_ReturnsEmpty()
    {
        var state   = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);   // only own pawn on board, no enemy
        state = state.WithHand(0, JackCard);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, JackCard);

        Assert.Empty(moves);
    }
}
