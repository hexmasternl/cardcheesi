## Context

The three Game projects (`CardCheesi.Game.Abstractions`, `CardCheesi.Game`, `CardCheesi.Game.Tests`) are completely empty scaffolds. The API project contains only the WeatherForecast template. The full game rules are documented in `docs/rules/`. All backend work — game logic, API endpoints, test scenarios — is blocked until a shared domain vocabulary exists.

Target runtime: .NET 10, nullable enabled, C# records preferred for value semantics. Tests use xUnit, Moq, and Bogus.

## Goals / Non-Goals

**Goals:**
- Define all game state as immutable C# records/enums in `CardCheesi.Game.Abstractions`.
- Add a `GameFactory` in `CardCheesi.Game` that constructs a valid initial `GameState`.
- Wire project references: `Game` → `Abstractions`, `Tests` → `Game`, `Api` → `Game`.
- Remove the WeatherForecast template from the API and add a stub `/game` endpoint.
- Add unit tests covering `GameFactory` construction invariants.

**Non-Goals:**
- Move validation or card effect application (future change).
- Persistence / database / EF Core.
- API DTOs or OpenAPI contracts (separate concern).
- SignalR / real-time game synchronisation.
- More than one game session in memory at once.

## Decisions

### 1. All domain types as C# `record` in `CardCheesi.Game.Abstractions`

**Decision**: Use C# `record` (or `record struct` for small value objects) for all domain types.

**Rationale**: Records give structural equality and immutability by default — essential for a functional-style game state that is replaced rather than mutated. Placing them in `Abstractions` makes them the shared contract for API, Game, and any future consumers without circular dependencies.

---

### 2. Pawn location as a sealed discriminated union

**Decision**: Represent a pawn's location with a sealed class hierarchy: `PawnLocation` (abstract base) → `ReserveLocation`, `BoardLocation(int Position)`, `FinishLocation(int Slot)`.

**Rationale**: Pawns exist in three fundamentally different location spaces. A single `int` with an offset (e.g. 100+ = finish) is fragile and leaks implementation detail. A sealed hierarchy is exhaustively matchable with C# `switch` and is self-documenting.

**Alternatives considered**: `Position` record with `PositionType` enum — verbose; pattern matching is less ergonomic.

---

### 3. `PawnStatus` enum for fast aggregate queries

**Decision**: Store `PawnStatus` (`Reserve`, `InPlay`, `Finished`) on each `Pawn` alongside its `PawnLocation`.

**Rationale**: Queries like "are all a player's pawns finished?" must be O(1) without inspecting position values. The enum is redundant with location but intentionally so for query performance.

---

### 4. Card as a value record — no rules coupling

**Decision**: `Card` is `record(CardSuit Suit, CardRank Rank)` only. Card effects are derived from `CardRank` at the game-logic layer, not stored on `Card`.

**Rationale**: Keeps the domain model free of behavioural coupling. A `Card` is data; its rules interpretation belongs in a `CardEffectResolver` (future change).

---

### 5. `IRandom` abstraction for deterministic test decks

**Decision**: `Deck.Shuffle(IRandom rng)` accepts an `IRandom` interface wrapping `System.Random`, so tests can inject a seeded or scripted RNG.

**Rationale**: Without this, shuffle tests are non-deterministic. Using `Random.Shared` as the production default requires no configuration.

---

### 6. `GameFactory.Create(IReadOnlyList<string> playerNames)` as the single construction entry point

**Decision**: All `GameState` instances are produced by `GameFactory`. The constructor validates exactly 4 player names and throws `ArgumentException` otherwise.

**Rationale**: Centralises invariant enforcement; prevents partially-constructed state from being passed around.

## Risks / Trade-offs

- [Immutable records produce new objects on every state change] → Acceptable for a single in-memory game session; revisit with `record struct` if hot-path profiling reveals pressure.
- [Sealed location hierarchy adds file count] → Mitigation: Group all `PawnLocation` subtypes in a single `PawnLocation.cs` file.
- [`GameFactory` hard-coded to exactly 4 players] → By design; throw `ArgumentException` with a clear message if violated.

## Migration Plan

1. Add project references (`.csproj` edits).
2. Create domain types in `CardCheesi.Game.Abstractions`.
3. Implement `GameFactory` and `IRandom` in `CardCheesi.Game`.
4. Add unit tests in `CardCheesi.Game.Tests`.
5. Remove WeatherForecast template and add stub `/game` endpoint in `CardCheesi.Game.Api`.
6. Run `dotnet build src/card-cheesi.slnx` and `dotnet test` to verify green.

Rollback: revert the four `.csproj` files and delete the new source files — no database migrations or infrastructure changes involved.

## Open Questions

- Should `Card` carry a `Guid` identity (useful for tracking which physical card was played), or is `(Suit, Rank)` sufficient as the key? *(Suggest: no identity for now — same rank/suit can appear once per deck.)*
