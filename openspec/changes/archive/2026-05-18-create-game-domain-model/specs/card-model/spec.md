# Spec: card-model

## Overview

Defines the card types used in CardCheesi. Cards are pure data — no rules are embedded in the card model itself.

## Types

### `CardSuit` (enum)

```csharp
public enum CardSuit { Clubs, Diamonds, Hearts, Spades }
```

### `CardRank` (enum)

```csharp
public enum CardRank { Ace = 1, Two, Three, Four, Five, Six, Seven, Eight, Nine, Ten, Jack, Queen, King }
```

### `Card` (record)

```
CardSuit Suit
CardRank Rank
```

Structural equality by `(Suit, Rank)`. No identity; a standard deck contains exactly one card per `(Suit, Rank)` combination.

### `Deck` (record)

```
IReadOnlyList<Card> Cards
```

- `Deck.Standard()` — returns a new, unshuffled deck of 52 cards (all suits × all ranks, ordered by suit then rank)
- `Deck Shuffle(IRandom rng)` — returns a new shuffled `Deck` using the Fisher-Yates algorithm

### `PlayerHand` (record)

```
Guid PlayerId
IReadOnlyList<Card> Cards
```

Represents the cards currently held by a single player in a round.

## `IRandom` (interface, in `CardCheesi.Game`)

```csharp
public interface IRandom
{
    int Next(int minValue, int maxValue);
}
```

Production implementation: `SystemRandom` wrapping `Random.Shared`. Injected into `Deck.Shuffle` and `GameFactory.Create` to allow deterministic tests.

## Notes

- `Deck.Shuffle` must not mutate state; it returns a new `Deck` record.
- The Fisher-Yates shuffle iterates from end to start: `for i from n-1 downto 1, swap cards[i] with cards[rng.Next(0, i+1)]`.
