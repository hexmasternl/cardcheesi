## Purpose

Define the canonical card domain model used by the game backend.

## Requirements

### Requirement: Standard cards use suit and rank enums
The system SHALL define `CardSuit` and `CardRank` enums for a standard 52-card deck.

#### Scenario: Card suits cover the standard four suits
- **WHEN** `CardSuit` is defined
- **THEN** it contains `Clubs`, `Diamonds`, `Hearts`, and `Spades`

#### Scenario: Card ranks cover ace through king
- **WHEN** `CardRank` is defined
- **THEN** it contains `Ace = 1`, `Two`, `Three`, `Four`, `Five`, `Six`, `Seven`, `Eight`, `Nine`, `Ten`, `Jack`, `Queen`, and `King`

### Requirement: Cards are pure value objects
The system SHALL define a `Card` record with `Suit` and `Rank` and no separate identity.

#### Scenario: Card record contains suit and rank
- **WHEN** a card is represented in memory
- **THEN** the `Card` record contains `Suit` and `Rank`

#### Scenario: Card equality is structural
- **WHEN** two cards have the same `Suit` and `Rank`
- **THEN** they are equal by value
- **AND** a standard deck contains exactly one card for each `(Suit, Rank)` combination

### Requirement: Deck represents an ordered collection of cards
The system SHALL define a `Deck` record with a `Cards` collection and operations to create and shuffle a standard deck.

#### Scenario: Standard deck contains all 52 cards in deterministic order
- **WHEN** `Deck.Standard()` is called
- **THEN** it returns a new deck containing 52 cards
- **AND** the cards are ordered by suit and then rank

#### Scenario: Shuffle returns a new deck without mutating the source
- **WHEN** `Deck.Shuffle(IRandom rng)` is called
- **THEN** it returns a new `Deck`
- **AND** it does not mutate the original deck instance

#### Scenario: Shuffle uses Fisher-Yates with the provided random source
- **WHEN** `Deck.Shuffle(IRandom rng)` is called
- **THEN** it performs a Fisher-Yates shuffle from the end of the collection to the start
- **AND** each swap index is selected using `rng.Next(0, i + 1)`

### Requirement: Player hands group cards by player
The system SHALL define a `PlayerHand` record with `PlayerId` and `Cards`.

#### Scenario: Player hand identifies card ownership
- **WHEN** a player's dealt cards are represented
- **THEN** the `PlayerHand` record contains the owning `PlayerId`
- **AND** the current `Cards` held by that player

### Requirement: Randomness is abstracted for deterministic tests
The system SHALL define an `IRandom` interface in `CardCheesi.Game` with `int Next(int minValue, int maxValue)` and provide `SystemRandom` as the production implementation.

#### Scenario: Shuffle depends on an injectable random source
- **WHEN** deck shuffling or game creation requires randomness
- **THEN** the implementation uses `IRandom`
- **AND** tests can provide deterministic random behavior

#### Scenario: Production random implementation wraps framework randomness
- **WHEN** the production application requests randomness
- **THEN** `SystemRandom` delegates to `Random.Shared`
