using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>
/// Tests for <see cref="GameState.HasPlayableCards"/> and <see cref="GameState.GetValidMoves"/>,
/// as well as <see cref="GameState.PlayCard"/>.
/// </summary>
public class PlayableCardDetectionTests
{
    private const string Code = "PLAY01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    // -----------------------------------------------------------------------
    // HasPlayableCards
    // -----------------------------------------------------------------------

    [Fact]
    public void HasPlayableCards_ReservePawnAndAceInHand_ReturnsTrue()
    {
        var state    = CreateGame();
        var aceCard  = new Card(CardSuit.Hearts, CardRank.Ace);
        state = state.WithHand(0, aceCard);

        Assert.True(state.HasPlayableCards(state.Players[0].Id));
    }

    [Fact]
    public void HasPlayableCards_BoardPawnAndTwoCard_ReturnsTrue()
    {
        var state   = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var twoCard = new Card(CardSuit.Hearts, CardRank.Two);
        state = state.WithHand(0, twoCard);

        Assert.True(state.HasPlayableCards(state.Players[0].Id));
    }

    [Fact]
    public void HasPlayableCards_AllReservePawnsAndNumericCard_ReturnsFalse()
    {
        // All pawns in reserve; a Two card needs a board pawn → no valid moves.
        var state   = CreateGame();
        var twoCard = new Card(CardSuit.Hearts, CardRank.Two);
        state = state.WithHand(0, twoCard);

        Assert.False(state.HasPlayableCards(state.Players[0].Id));
    }

    [Fact]
    public void HasPlayableCards_AllOwnPawnsFinished_ProxiesTeammate_ReturnsFalse_WhenTeammateInReserve()
    {
        // Player[0] proxies player[2] (teammate). Teammate has only reserve pawns. Four card → no retreat moves.
        var state = CreateGame();
        state = state
            .WithPawnInFinish(0, 0, 1)
            .WithPawnInFinish(0, 1, 2)
            .WithPawnInFinish(0, 2, 3)
            .WithPawnInFinish(0, 3, 4);

        var fourCard = new Card(CardSuit.Hearts, CardRank.Four);
        state = state.WithHand(0, fourCard);

        Assert.False(state.HasPlayableCards(state.Players[0].Id));
    }

    [Fact]
    public void HasPlayableCards_AllOwnPawnsFinished_ProxiesTeammate_ReturnsTrue_WhenTeammateHasBoard()
    {
        // Player[0] proxies player[2] (teammate). Teammate has a board pawn. Two card → valid advance.
        var state = CreateGame();
        state = state
            .WithPawnInFinish(0, 0, 1)
            .WithPawnInFinish(0, 1, 2)
            .WithPawnInFinish(0, 2, 3)
            .WithPawnInFinish(0, 3, 4);
        state = state.WithPawnAtBoard(2, 0, 5);   // teammate's pawn on board

        var twoCard = new Card(CardSuit.Hearts, CardRank.Two);
        state = state.WithHand(0, twoCard);

        Assert.True(state.HasPlayableCards(state.Players[0].Id));
    }

    [Fact]
    public void HasPlayableCards_NullHands_ReturnsFalse()
    {
        var state = new GameState(
            Guid.NewGuid(), "CODE01", GameStatus.InProgress,
            [], [], null, null, null);

        Assert.False(state.HasPlayableCards(Guid.NewGuid()));
    }

    // -----------------------------------------------------------------------
    // GetValidMoves — per-card enumeration
    // -----------------------------------------------------------------------

    [Fact]
    public void GetValidMoves_AceCard_FourReservePawns_ReturnsFourSingleMovesWithZeroSteps()
    {
        var state   = CreateGame();
        var aceCard = new Card(CardSuit.Spades, CardRank.Ace);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, aceCard);

        Assert.Equal(4, moves.Count);
        Assert.All(moves, m => Assert.IsType<SingleMove>(m));
        Assert.All(moves, m => Assert.Equal(0, ((SingleMove)m).Steps));
    }

    [Fact]
    public void GetValidMoves_KingCard_FourReservePawns_ReturnsFourSingleMoves()
    {
        var state    = CreateGame();
        var kingCard = new Card(CardSuit.Spades, CardRank.King);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, kingCard);

        Assert.Equal(4, moves.Count);
        Assert.All(moves, m => Assert.IsType<SingleMove>(m));
        Assert.All(moves, m => Assert.Equal(0, ((SingleMove)m).Steps));
    }

    [Fact]
    public void GetValidMoves_KingCard_NoBoardEntry_ReturnsNoMoves_WhenHomeOccupied()
    {
        // Enter pawn0 to home — now home is occupied. King cannot enter any pawn to that slot again.
        var state    = CreateGame();
        var pawn0    = state.Players[0].Pawns[0];
        state = state.MakeMove(pawn0.Id, 0);       // pawn0 at home (position 1), protected

        var kingCard = new Card(CardSuit.Spades, CardRank.King);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, kingCard);

        // pawn0 is now at home (position 1) and is protected.
        // CanEnterHome checks: no own pawn may already occupy the home position.
        // All 3 remaining reserve pawns share the same home position → all blocked.
        Assert.Equal(0, moves.Count);
    }

    [Fact]
    public void GetValidMoves_QueenCard_BoardPawn_Returns12StepMove()
    {
        var state    = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 5);
        var queenCard = new Card(CardSuit.Hearts, CardRank.Queen);
        var playerId  = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, queenCard);

        Assert.Contains(moves, m => m is SingleMove sm && sm.Steps == 12);
    }

    [Fact]
    public void GetValidMoves_TwoCard_NoMoves_WhenAllReserve()
    {
        var state   = CreateGame();
        var twoCard = new Card(CardSuit.Hearts, CardRank.Two);
        var playerId = state.Players[0].Id;

        var moves = state.GetValidMoves(playerId, twoCard);

        Assert.Empty(moves);
    }

    [Fact]
    public void GetValidMoves_AceCard_BoardPawnAdvancesOne()
    {
        var state   = CreateGame();
        state = state.WithPawnAtBoard(0, 0, 10);
        var aceCard = new Card(CardSuit.Hearts, CardRank.Ace);
        var playerId = state.Players[0].Id;
        var pawnId   = state.Players[0].Pawns[0].Id;

        var moves = state.GetValidMoves(playerId, aceCard);

        Assert.Contains(moves, m => m is SingleMove sm && sm.PawnId == pawnId && sm.Steps == 1);
    }
}

/// <summary>Tests for <see cref="GameState.PlayCard"/> hand management.</summary>
public class PlayCardTests
{
    private const string Code = "PLCD01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    [Fact]
    public void PlayCard_RemovesCardFromHand()
    {
        var state    = CreateGame();
        var playerId = state.Players[0].Id;
        var aceCard  = new Card(CardSuit.Hearts, CardRank.Ace);
        var twoCard  = new Card(CardSuit.Spades, CardRank.Two);
        state = state.WithHand(0, aceCard, twoCard);

        state = state.PlayCard(playerId, aceCard);

        var hand = state.Hands!.Single(h => h.PlayerId == playerId);
        Assert.Equal(1, hand.Cards.Count);
        Assert.DoesNotContain(aceCard, hand.Cards);
        Assert.Contains(twoCard, hand.Cards);
    }

    [Fact]
    public void PlayCard_OnlyRemovesFirstOccurrenceOfDuplicateCard()
    {
        var state    = CreateGame();
        var playerId = state.Players[0].Id;
        var aceCard  = new Card(CardSuit.Hearts, CardRank.Ace);
        state = state.WithHand(0, aceCard, aceCard);  // two identical aces

        state = state.PlayCard(playerId, aceCard);

        var hand = state.Hands!.Single(h => h.PlayerId == playerId);
        Assert.Equal(1, hand.Cards.Count);             // one ace remains
        Assert.Contains(aceCard, hand.Cards);
    }

    [Fact]
    public void PlayCard_CardNotInHand_ThrowsInvalidOperation()
    {
        var state    = CreateGame();
        var playerId = state.Players[0].Id;
        var aceCard  = new Card(CardSuit.Hearts, CardRank.Ace);
        var twoCard  = new Card(CardSuit.Spades, CardRank.Two);
        state = state.WithHand(0, twoCard);            // hand has only Two

        Assert.Throws<InvalidOperationException>(() => state.PlayCard(playerId, aceCard));
    }

    [Fact]
    public void PlayCard_NullHands_ThrowsInvalidOperation()
    {
        var state = new GameState(
            Guid.NewGuid(), "CODE01", GameStatus.InProgress,
            [], [], null, null, null);

        Assert.Throws<InvalidOperationException>(() => state.PlayCard(Guid.NewGuid(), new Card(CardSuit.Hearts, CardRank.Ace)));
    }

    [Fact]
    public void PlayCard_OtherPlayersHandsUnchanged()
    {
        var state    = CreateGame();
        var playerId = state.Players[0].Id;
        var aceCard  = new Card(CardSuit.Hearts, CardRank.Ace);
        state = state.WithHand(0, aceCard);

        var handsBefore = state.Hands!.Where(h => h.PlayerId != playerId).ToList();
        state = state.PlayCard(playerId, aceCard);
        var handsAfter  = state.Hands!.Where(h => h.PlayerId != playerId).ToList();

        for (int i = 0; i < handsBefore.Count; i++)
            Assert.Equal(handsBefore[i].Cards.Count, handsAfter[i].Cards.Count);
    }
}
