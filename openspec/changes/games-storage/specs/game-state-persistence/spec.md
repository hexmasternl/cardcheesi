## ADDED Requirements

### Requirement: Games table schema
The `Games` table in PostgreSQL SHALL have exactly three columns: `Id` (UUID, primary key), `GameCode` (VARCHAR(6), unique not-null), and `State` (JSONB, not-null). No other columns SHALL exist on this table.

#### Scenario: Table created by migration
- **WHEN** the EF Core migration `AddGamesTable` is applied
- **THEN** the `Games` table exists with columns `Id UUID PRIMARY KEY`, `GameCode VARCHAR(6) NOT NULL UNIQUE`, and `State JSONB NOT NULL`

#### Scenario: Unique index enforced at database level
- **WHEN** two rows with the same `GameCode` are inserted concurrently
- **THEN** the database rejects the second insert with a unique-constraint violation

---

### Requirement: GameState JSONB serialization
The `State` column SHALL store a JSON-serialized representation of the full `GameState` domain object. The serialization format SHALL be compatible with `System.Text.Json`. Deserialization SHALL reconstruct an object that is value-equal to the original.

#### Scenario: Round-trip serialization
- **WHEN** a `GameState` is saved via `IGameRepository.SaveAsync`
- **THEN** loading it back via `GetByIdAsync` returns a `GameState` that is structurally equal to the one that was saved

#### Scenario: PawnLocation discriminated union survives round-trip
- **WHEN** a `GameState` containing pawns in all three location types (`ReserveLocation`, `BoardLocation`, `FinishLocation`) is saved
- **THEN** loading it back correctly reconstructs each pawn's location type and value

---

### Requirement: IGameRepository abstraction
The system SHALL expose an `IGameRepository` interface in `CardCheesi.Game.Abstractions` with the following operations:
- `SaveAsync(GameState game, CancellationToken ct)` — insert or update
- `GetByIdAsync(Guid id, CancellationToken ct)` — returns `GameState?`
- `GetByCodeAsync(string code, CancellationToken ct)` — returns `GameState?`

#### Scenario: Save new game
- **WHEN** `SaveAsync` is called with a `GameState` whose `Id` does not exist in the database
- **THEN** a new row is inserted with the correct `Id`, `GameCode`, and serialized `State`

#### Scenario: Update existing game
- **WHEN** `SaveAsync` is called with a `GameState` whose `Id` already exists
- **THEN** the existing row's `State` column is updated to the new serialized value

#### Scenario: Get by ID — found
- **WHEN** `GetByIdAsync` is called with a GUID that matches an existing row
- **THEN** the deserialized `GameState` is returned

#### Scenario: Get by ID — not found
- **WHEN** `GetByIdAsync` is called with a GUID that does not match any row
- **THEN** `null` is returned

#### Scenario: Get by code — found
- **WHEN** `GetByCodeAsync` is called with a 6-character code that matches an existing row
- **THEN** the deserialized `GameState` is returned

#### Scenario: Get by code — not found
- **WHEN** `GetByCodeAsync` is called with a code that does not match any row
- **THEN** `null` is returned
