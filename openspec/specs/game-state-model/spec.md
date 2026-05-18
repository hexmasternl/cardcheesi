## Purpose

Define the canonical game-state domain model shared by backend components.

## Requirements

### Requirement: Pawn state is modeled explicitly
The system SHALL define a `PawnStatus` enum with the values `Reserve`, `InPlay`, and `Finished`.

#### Scenario: Pawn status identifies reserve pawns
- **WHEN** a pawn has not entered the game board
- **THEN** its status is `Reserve`

#### Scenario: Pawn status identifies active pawns
- **WHEN** a pawn occupies a board position
- **THEN** its status is `InPlay`

#### Scenario: Pawn status identifies completed pawns
- **WHEN** a pawn is in its owner's finish track
- **THEN** its status is `Finished`

### Requirement: Pawn location uses a closed hierarchy
The system SHALL model pawn position as a `PawnLocation` sealed hierarchy with the concrete types `ReserveLocation`, `BoardLocation(int Position)`, and `FinishLocation(int Slot)`.

#### Scenario: Reserve location represents off-board pawns
- **WHEN** a pawn is not yet in play
- **THEN** its location is `ReserveLocation`

#### Scenario: Board location constrains shared-loop positions
- **WHEN** a pawn is on the shared board loop
- **THEN** its location is `BoardLocation` with a `Position` in the range 1 through 64

#### Scenario: Finish location constrains finish slots
- **WHEN** a pawn is in a personal finish track
- **THEN** its location is `FinishLocation` with a `Slot` in the range 1 through 4

### Requirement: Core game records define the running game state
The system SHALL define immutable records for `Pawn`, `Player`, `Team`, and `GameState` in `CardCheesi.Game.Abstractions`.

#### Scenario: Pawn record contains identity and location data
- **WHEN** a pawn is represented in memory
- **THEN** the `Pawn` record contains `Id`, `OwnerId`, `Status`, and `Location`

#### Scenario: Player record contains exactly four pawns
- **WHEN** a player is represented in memory
- **THEN** the `Player` record contains `Id`, `Name`, and `Pawns`
- **AND** `Pawns` contains exactly 4 items

#### Scenario: Team record contains exactly two players
- **WHEN** a team is represented in memory
- **THEN** the `Team` record contains `Id` and `Players`
- **AND** `Players` contains exactly 2 items

#### Scenario: Game state contains all runtime aggregates
- **WHEN** a full game is represented in memory
- **THEN** the `GameState` record contains `Id`, `Teams`, `Players`, `Turn`, `Deck`, and `Hands`
- **AND** `Players` is a flat list ordered by turn sequence

### Requirement: Game state invariants are enforced
The system SHALL enforce the initial game-state invariants for player count, team assignment, and pawn initialization.

#### Scenario: New game contains four players on two teams
- **WHEN** a game state is initialized
- **THEN** it contains exactly 4 players and 2 teams
- **AND** each team contains exactly 2 players

#### Scenario: Team assignments follow turn order
- **WHEN** a game state is initialized
- **THEN** Team A contains `Players[0]` and `Players[2]`
- **AND** Team B contains `Players[1]` and `Players[3]`

#### Scenario: New players start with all pawns in reserve
- **WHEN** a game state is initialized
- **THEN** each player has exactly 4 pawns
- **AND** each pawn has status `Reserve`
- **AND** each pawn location is `ReserveLocation`

### Requirement: Game factory creates a valid initial game
The system SHALL provide `GameFactory.Create(IReadOnlyList<string> playerNames)` in `CardCheesi.Game` to construct a fully initialized `GameState`.

#### Scenario: Factory rejects invalid player counts
- **WHEN** `GameFactory.Create` is called with anything other than exactly 4 player names
- **THEN** it throws `ArgumentException`

#### Scenario: Factory assigns identifiers and reserve pawns
- **WHEN** `GameFactory.Create` is called with exactly 4 player names
- **THEN** each player receives a new `Guid` identifier
- **AND** each player receives 4 pawn instances owned by that player
- **AND** each pawn starts in reserve

#### Scenario: Factory builds teams from alternating players
- **WHEN** `GameFactory.Create` constructs a new game
- **THEN** it builds Team A from players at indexes 0 and 2
- **AND** it builds Team B from players at indexes 1 and 3

#### Scenario: Factory initializes shuffled deck and supporting state
- **WHEN** `GameFactory.Create` constructs a new game
- **THEN** it shuffles a standard deck using `IRandom`
- **AND** it returns a fully constructed `GameState`
