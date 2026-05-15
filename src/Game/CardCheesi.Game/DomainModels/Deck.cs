using CardCheesi.Game.Abstractions.DomainModels;

namespace CardCheesi.Game.DomainModels;

public record Deck(IReadOnlyList<Card> Cards) : IDeck
{
    /// <summary>Creates an unshuffled standard deck of 52 cards (ordered by suit then rank).</summary>
    public static Deck Standard()
    {
        var cards = new List<Card>(52);
        foreach (CardSuit suit in Enum.GetValues<CardSuit>())
        foreach (CardRank rank in Enum.GetValues<CardRank>())
            cards.Add(new Card(suit, rank));
        return new Deck(cards.AsReadOnly());
    }

    /// <summary>Returns a new <see cref="Deck"/> with cards shuffled using Fisher-Yates.</summary>
    public Deck Shuffle(IRandom rng)
    {
        var cards = new List<Card>(Cards);
        for (var i = cards.Count - 1; i > 0; i--)
        {
            var j = rng.Next(0, i + 1);
            (cards[i], cards[j]) = (cards[j], cards[i]);
        }
        return new Deck(cards.AsReadOnly());
    }

    IDeck IDeck.Shuffle(IRandom rng) => Shuffle(rng);

    /// <summary>
    /// Deals <paramref name="count"/> cards from the top of the deck.
    /// Returns the dealt cards and a new <see cref="Deck"/> with those cards removed.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="count"/> is negative or exceeds the number of cards in the deck.
    /// </exception>
    public (IReadOnlyList<Card> Dealt, Deck Remaining) Deal(int count)
    {
        if (count < 0 || count > Cards.Count)
            throw new ArgumentOutOfRangeException(nameof(count),
                $"Cannot deal {count} cards from a deck of {Cards.Count}.");

        var dealt = Cards.Take(count).ToList().AsReadOnly();
        var remaining = new Deck(Cards.Skip(count).ToList().AsReadOnly());
        return (dealt, remaining);
    }
}
