# Spec: game-state-model

## Overview

Defines the core game state records, enums, and value objects in `CardCheesi.Game.Abstractions`. These types represent the authoritative in-memory state of a running game.

## Types

### `PawnStatus` (enum)

```csharp
public enum PawnStatus { Reserve, InPlay, Finished }
```

### `PawnLocation` (sealed hierarchy)

Abstract base with three concrete subtypes in a single file:

- `ReserveLocation` — pawn is off the board
- `BoardLocation(int Position)` — position 1–64 on the shared board loop
- `FinishLocation(int Slot)` — slot 1–4 in a player's personal finish track

### `Pawn` (record)

```
Guid Id
Guid OwnerId
PawnStatus Status
PawnLocation Location
```

### `Player` (record)

```
Guid Id
string Name
IReadOnlyList<Pawn> Pawns  // always 4 pawns
```

### `Team` (record)

```
Guid Id
IReadOnlyList<Player> Players  // always 2 players
```

### `GameState` (record)

```
Guid Id
IReadOnlyList<Team> Teams          // always 2 teams
IReadOnlyList<Player> Players      // flat list of 4, ordered by turn
TurnState Turn
Deck Deck
IReadOnlyList<PlayerHand> Hands
```

## Invariants

- Exactly 4 players, 2 teams (2 players per team)
- Each player has exactly 4 pawns; all start in `ReserveLocation` with `PawnStatus.Reserve`
- Team A = Players[0] + Players[2]; Team B = Players[1] + Players[3]

## Factory

`GameFactory.Create(IReadOnlyList<string> playerNames)` in `CardCheesi.Game`:

- Validates exactly 4 names; throws `ArgumentException` otherwise
- Assigns `Player.Id = Guid.NewGuid()`
- Creates 4 `Pawn` instances per player (all in reserve)
- Builds 2 teams: A=(P0,P2), B=(P1,P3)
- Shuffles a standard deck using `IRandom`
- Returns fully-constructed `GameState`
