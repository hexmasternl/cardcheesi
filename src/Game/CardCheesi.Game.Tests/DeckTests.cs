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
    public void Shuffle_WithSameSeed_ProducesSameOrder()
    {
        var original = Deck.Standard();
        var deck1 = original.Shuffle(new SeededRandom(99));
        var deck2 = original.Shuffle(new SeededRandom(99));

        Assert.True(deck1.Cards.SequenceEqual(deck2.Cards));
    }
}
