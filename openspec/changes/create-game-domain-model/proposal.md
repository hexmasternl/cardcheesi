## Why

The game projects (`CardCheesi.Game`, `CardCheesi.Game.Abstractions`) are empty scaffolds with no domain types. Before any game logic, API endpoints, or tests can be written, the core domain model must exist as the shared vocabulary of the entire backend.

## What Changes

- Define all core domain records, enums, and value objects in `CardCheesi.Game.Abstractions` — the project that the API and any future consumers reference.
- Implement a `GameFactory` in `CardCheesi.Game` that constructs a valid initial `GameState` from a set of player names.
- Add a `ProjectReference` from `CardCheesi.Game` → `CardCheesi.Game.Abstractions` and from `CardCheesi.Game.Tests` → `CardCheesi.Game`.
- Replace the placeholder WeatherForecast code in `CardCheesi.Game.Api` with a stub `/game` endpoint wired to the domain types.
- Add unit tests in `CardCheesi.Game.Tests` covering `GameFactory` and core value-object invariants.

## Capabilities

### New Capabilities

- `game-state-model`: Immutable records representing a running game: `GameState`, `Player`, `Team`, `Pawn`, `PawnStatus` (reserve / in-play / finished), and board position as a value type.
- `card-model`: `Card` record with `CardSuit` and `CardRank` enums, `Deck` (ordered collection of 52 cards), and `PlayerHand` (the cards held by one player during a round).
- `turn-state-model`: `TurnState` record capturing the active player, current dealer, round number (1–3), and the per-round dealing schedule (5 / 4 / 4 cards).

### Modified Capabilities

<!-- No existing specs to modify. -->

## Impact

- **`CardCheesi.Game.Abstractions`**: new namespace `CardCheesi.Game.Abstractions` — all domain records, enums, value types.
- **`CardCheesi.Game`**: new `GameFactory` class; project reference to `Abstractions`.
- **`CardCheesi.Game.Tests`**: new unit tests; project reference to `CardCheesi.Game`.
- **`CardCheesi.Game.Api`**: WeatherForecast template removed; project reference to `CardCheesi.Game` added; stub `/game` endpoint added.
- No frontend or Aspire changes required.
