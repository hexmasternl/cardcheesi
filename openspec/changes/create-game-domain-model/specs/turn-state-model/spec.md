# Spec: turn-state-model

## Overview

Defines `TurnState`, the record that tracks who is the current active player, who is the dealer, which round of dealing is active, and how many cards are dealt per round.

## Types

### `TurnState` (record)

```
Guid ActivePlayerId
Guid DealerId
int RoundNumber          // 1, 2, or 3
int CardsThisRound       // derived: 5 for round 1, 4 for rounds 2 and 3
```

## Dealing Schedule

Each dealer turn consists of 3 rounds:

| Round | Cards dealt per player |
|-------|----------------------|
| 1     | 5                    |
| 2     | 4                    |
| 3     | 4                    |

After all 3 rounds the dealer rotates clockwise (to the next player in `GameState.Players`).

## Invariants

- `RoundNumber` is always 1, 2, or 3.
- `CardsThisRound` is a derived value: `RoundNumber == 1 ? 5 : 4`.
- The initial `TurnState` for a new game: `ActivePlayerId = Players[0].Id`, `DealerId = Players[0].Id`, `RoundNumber = 1`.

## Notes

- `TurnState` is immutable; advancing the turn produces a new `TurnState` instance.
- The `CardsThisRound` property may be computed in the record's constructor or as an `init`-only property.
