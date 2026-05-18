using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;
using CardCheesi.Game.Tests.TestHelpers;

namespace CardCheesi.Game.Tests;

/// <summary>
/// Tests for <see cref="GameState.DisposeHand"/>: a player must take a turn whenever they
/// have at least one playable card; only when no card is playable may they discard, and
/// when they do, the entire hand must be discarded in one operation.
/// </summary>
public class DisposeHandTests
{
    private const string Code = "DISP01";

    private static GameState CreateGame()
        => GameFactory.Create(["Alice", "Bob", "Carol", "Dave"], Code);

    [Fact]
    public void DisposeHand_AllCardsUnplayable_ClearsEntireHand()
    {
        // All pawns in reserve + only numeric cards (which need board pawns) → no moves.
        var state    = CreateGame();
        var two      = new Card(CardSuit.Hearts, CardRank.Two);
        var three    = new Card(CardSuit.Spades, CardRank.Three);
        var queen    = new Card(CardSuit.Clubs,  CardRank.Queen);
        state = state.WithHand(0, two, three, queen);
        var playerId = state.Players[0].Id;

        state = state.DisposeHand(playerId);

        var hand = state.Hands!.Single(h => h.PlayerId == playerId);
        Assert.Empty(hand.Cards);
    }

    [Fact]
    public void DisposeHand_AnyPlayableCard_Throws()
    {
        // Ace is always playable (pawn in reserve → can enter home), so dispose must be rejected
        // even when other cards in the hand are unplayable.
        var state    = CreateGame();
        var ace      = new Card(CardSuit.Hearts, CardRank.Ace);
        var two      = new Card(CardSuit.Spades, CardRank.Two);
        state = state.WithHand(0, ace, two);
        var playerId = state.Players[0].Id;

        Assert.Throws<InvalidOperationException>(() => state.DisposeHand(playerId));
    }

    [Fact]
    public void DisposeHand_DoesNotAffectOtherPlayersHands()
    {
        var state    = CreateGame();
        var two      = new Card(CardSuit.Hearts, CardRank.Two);
        var three    = new Card(CardSuit.Spades, CardRank.Three);
        state = state.WithHand(0, two);          // player 0: only Two — unplayable
        state = state.WithHand(1, three);        // player 1: only Three — also unplayable, but we don't dispose it
        var playerId = state.Players[0].Id;
        var otherId  = state.Players[1].Id;

        state = state.DisposeHand(playerId);

        Assert.Empty(state.Hands!.Single(h => h.PlayerId == playerId).Cards);
        Assert.Single(state.Hands!.Single(h => h.PlayerId == otherId).Cards);
    }

    [Fact]
    public void DisposeHand_EmptyHand_IsNoOp()
    {
        var state    = CreateGame();
        state = state.WithHand(0); // no cards
        var playerId = state.Players[0].Id;

        var result = state.DisposeHand(playerId);

        Assert.Empty(result.Hands!.Single(h => h.PlayerId == playerId).Cards);
    }

    [Fact]
    public void DisposeHand_NullHands_Throws()
    {
        var state = new GameState(
            Guid.NewGuid(), "CODE01", GameStatus.InProgress,
            [], [], null, null, null);

        Assert.Throws<InvalidOperationException>(() => state.DisposeHand(Guid.NewGuid()));
    }

    [Fact]
    public void DisposeHand_PlayerNotInGame_Throws()
    {
        var state    = CreateGame();
        var two      = new Card(CardSuit.Hearts, CardRank.Two);
        state = state.WithHand(0, two);

        Assert.Throws<InvalidOperationException>(() => state.DisposeHand(Guid.NewGuid()));
    }
}
