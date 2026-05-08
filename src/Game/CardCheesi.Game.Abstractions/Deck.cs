namespace CardCheesi.Game.Abstractions;

public record Deck(IReadOnlyList<Card> Cards)
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
}
