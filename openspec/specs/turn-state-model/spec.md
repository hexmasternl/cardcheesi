## Purpose

Define the canonical turn-state model for dealer rotation and round-based dealing.

## Requirements

### Requirement: Turn state identifies the active player and dealer
The system SHALL define an immutable `TurnState` record containing `ActivePlayerId`, `DealerId`, `RoundNumber`, and `CardsThisRound`.

#### Scenario: Turn state captures current actors
- **WHEN** turn state is represented in memory
- **THEN** it contains the current `ActivePlayerId`
- **AND** the current `DealerId`

#### Scenario: Turn state captures round metadata
- **WHEN** turn state is represented in memory
- **THEN** it contains `RoundNumber`
- **AND** `CardsThisRound`

### Requirement: Dealer turns follow a 5/4/4 dealing schedule
The system SHALL model each dealer turn as three rounds that deal 5 cards in round 1 and 4 cards in rounds 2 and 3.

#### Scenario: First round deals five cards per player
- **WHEN** `RoundNumber` is 1
- **THEN** `CardsThisRound` is 5

#### Scenario: Second and third rounds deal four cards per player
- **WHEN** `RoundNumber` is 2 or 3
- **THEN** `CardsThisRound` is 4

### Requirement: Turn-state invariants are enforced
The system SHALL restrict `RoundNumber` to 1 through 3 and derive `CardsThisRound` from the round number.

#### Scenario: Round number stays within valid bounds
- **WHEN** a `TurnState` instance is created or advanced
- **THEN** `RoundNumber` is always 1, 2, or 3

#### Scenario: Cards this round is derived from the round number
- **WHEN** `RoundNumber` changes
- **THEN** `CardsThisRound` is computed as `RoundNumber == 1 ? 5 : 4`

### Requirement: New games start on the first player's first dealing round
The system SHALL initialize a new game with `ActivePlayerId = Players[0].Id`, `DealerId = Players[0].Id`, and `RoundNumber = 1`.

#### Scenario: New game uses the first player as dealer and active player
- **WHEN** a new `GameState` is created
- **THEN** `TurnState.ActivePlayerId` equals `Players[0].Id`
- **AND** `TurnState.DealerId` equals `Players[0].Id`
- **AND** `TurnState.RoundNumber` is 1

### Requirement: Advancing turn state preserves immutability and dealer rotation
The system SHALL advance turn state by creating a new `TurnState` instance and rotate the dealer clockwise after the third dealing round.

#### Scenario: Advancing the turn creates a new immutable state
- **WHEN** turn progression occurs
- **THEN** it produces a new `TurnState` instance
- **AND** it does not mutate the previous instance

#### Scenario: Dealer rotates after round three completes
- **WHEN** the third dealing round completes
- **THEN** the next dealer is the next player in `GameState.Players`
- **AND** the next dealer is selected clockwise from the current dealer
