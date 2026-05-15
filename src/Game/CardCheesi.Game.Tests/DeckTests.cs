using CardCheesi.Game.Abstractions.DomainModels;
using CardCheesi.Game.DomainModels;

namespace CardCheesi.Game.Tests;

public sealed class DeckTests
{
    [Fact]
    public void Standard_Returns52Cards()
    {
        var deck = Deck.Standard();

        Assert.Equal(52, deck.Cards.Count);
    }

    [Fact]
    public void Standard_ContainsNoDuplicates()
    {
        var deck = Deck.Standard();

        var unique = deck.Cards.Distinct().Count();
        Assert.Equal(52, unique);
    }

    [Fact]
    public void Standard_ContainsAllSuitsAndRanks()
    {
        var deck = Deck.Standard();

        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        foreach (CardRank rank in Enum.GetValues<CardRank>())
            Assert.Contains(new Card(suit, rank), deck.Cards);
    }

    [Fact]
    public void Shuffle_ReturnsNewDeckWithSameCards()
    {
        var original = Deck.Standard();
        var shuffled = original.Shuffle(new SeededRandom(42));

        Assert.Equal(52, shuffled.Cards.Count);
        Assert.All(original.Cards, c => Assert.Contains(c, shuffled.Cards));
    }

    [Fact]
    public void Shuffle_DoesNotMutateOriginalDeck()
    {
        var original = Deck.Standard();
        var originalCards = original.Cards.ToList();

        original.Shuffle(new SeededRandom(42));

        Assert.Equal(originalCards, original.Cards);
    }

    [Fact]
    public void Shuffle_ProducesDifferentOrderThanOriginal()
    {
        var original = Deck.Standard();
        var shuffled = original.Shuffle(new SeededRandom(42));

        // With 52 cards and a seeded shuffle, the order should differ
        Assert.False(original.Cards.SequenceEqual(shuffled.Cards),
            "Shuffled deck should not be in the same order as original.");
    }

    [Fact]
    public void Deal_ReturnsRequestedNumberOfCards()
    {
        var deck = Deck.Standard().Shuffle(new SeededRandom(1));

        var (dealt, _) = deck.Deal(5);

        Assert.Equal(5, dealt.Count);
    }

    [Fact]
    public void Deal_RemainingDeckHasReducedCount()
    {
        var deck = Deck.Standard().Shuffle(new SeededRandom(1));

        var (_, remaining) = deck.Deal(5);

        Assert.Equal(47, remaining.Cards.Count);
    }

    [Fact]
    public void Deal_DealtCardsAreFromTopOfDeck()
    {
        var deck = Deck.Standard().Shuffle(new SeededRandom(1));

        var (dealt, _) = deck.Deal(5);

        Assert.Equal(deck.Cards.Take(5).ToList(), dealt);
    }

    [Fact]
    public void Deal_RemainingCardsMatchDeckTail()
    {
        var deck = Deck.Standard().Shuffle(new SeededRandom(1));

        var (_, remaining) = deck.Deal(5);

        Assert.Equal(deck.Cards.Skip(5).ToList(), remaining.Cards);
    }

    [Fact]
    public void Deal_DoesNotMutateOriginalDeck()
    {
        var deck = Deck.Standard();
        var originalCards = deck.Cards.ToList();

        deck.Deal(5);

        Assert.Equal(originalCards, deck.Cards);
    }

    [Fact]
    public void Deal_WithZeroCount_ReturnsEmptyDealtAndFullDeck()
    {
        var deck = Deck.Standard();

        var (dealt, remaining) = deck.Deal(0);

        Assert.Empty(dealt);
        Assert.Equal(52, remaining.Cards.Count);
    }

    [Fact]
    public void Deal_WithExactDeckCount_EmptiesRemainingDeck()
    {
        var deck = Deck.Standard();

        var (dealt, remaining) = deck.Deal(52);

        Assert.Equal(52, dealt.Count);
        Assert.Empty(remaining.Cards);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(53)]
    public void Deal_WithInvalidCount_ThrowsArgumentOutOfRangeException(int count)
    {
        var deck = Deck.Standard();

        Assert.Throws<ArgumentOutOfRangeException>(() => deck.Deal(count));
    }
}
