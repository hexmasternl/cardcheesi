## Why

The prior changes (`create-new-game`, `join-game`) implied a relational `Games` table but left the exact storage schema unspecified. Game state is a richly nested domain object that evolves frequently — mapping it to relational columns would require constant migrations. Storing the full `GameState` as a PostgreSQL `JSONB` column gives a stable table schema (just `Id` + `GameCode` + `State`) while retaining the ability to query and index into the game state document if needed.

## What Changes

- Define the `Games` table with exactly three columns: `Id` (UUID primary key), `GameCode` (varchar 6, unique index), and `State` (JSONB).
- Configure EF Core to serialize/deserialize `GameState` as JSONB via Npgsql's built-in JSON column mapping.
- Add an EF Core migration that creates the `Games` table with this schema.
- Implement an `IGameRepository` interface in `CardCheesi.Game.Abstractions` and its EF Core-backed implementation in `CardCheesi.Game`.
- Register `IGameRepository` in the API's DI container.
- Add unit tests for the repository (load, save, not-found) and JSONB round-trip integrity tests.

## Capabilities

### New Capabilities

- `game-state-persistence`: The `Games` table stores each game as `Id` (GUID PK), `GameCode` (6-char unique), and `State` (JSONB). The full `GameState` domain object is serialized to and deserialized from the `State` column. An `IGameRepository` abstraction provides `SaveAsync`, `GetByIdAsync`, and `GetByCodeAsync` operations.

### Modified Capabilities

- `game-creation`: Game creation now persists the new `GameState` through `IGameRepository` rather than ad-hoc EF entities; the `Games` table schema changes from a relational structure to the `Id` / `GameCode` / `State` layout described above.
- `join-game`: The join flow reads and writes `GameState` through `IGameRepository`, replacing any direct `DbContext` access; no additional columns are needed — the updated participant list is part of the serialized `State` JSONB.

## Impact

- **`CardCheesi.Game.Abstractions`**: new `IGameRepository` interface.
- **`CardCheesi.Game`**: new `GameRepository` EF Core implementation; Npgsql JSONB column mapping configured via `OnModelCreating`.
- **`CardCheesi.Game.Api`**: DI registration of `IGameRepository`; existing game-creation and join endpoints updated to use the repository; EF Core migration added.
- **`CardCheesi.Game.Tests`**: unit tests for `GameRepository` (mock DbContext + in-memory Npgsql); JSONB round-trip test asserting `GameState` survives serialization unchanged.
- **No Aspire, frontend, or auth changes** required.
