## Why

The game domain model has `PlayCard` and `MakeMove` as `NotImplementedException` stubs — no movement logic or rule enforcement exists. Without validation, illegal moves can be submitted and the system cannot determine whether a player has any playable cards (needed to decide if they must dispose their hand).

## What Changes

- Add `IsProtected` property to `IPawn` / `Pawn` (protected when newly placed at home, or once in the finish area).
- Add `HasPlayableCards(Guid playerId)` to `IGameState` — returns whether a player can legally play any card in their hand (drives `canDispose` in the turn SSE event).
- Add `GetValidMoves(Guid playerId, Card card)` to `IGameState` — returns the set of legal move options for a specific card, enabling UI hints and server-side validation.
- Implement `GameState.MakeMove` with full rule enforcement: board wrap-around (64 positions), finish area entry, hit resolution, cannot-pass-protected-pawn checks, reverse (Four), split (Seven), swap (Jack).
- Implement `GameState.PlayCard` to validate the card is in the player's hand and remove it, returning the updated state.
- Extend `IGameState.MakeMove` interface to support the Jack swap (two pawn IDs) and Seven split (two pawn/steps pairs) — **BREAKING** change to the method signature or addition of dedicated overloads.
- Wire `HasPlayableCards` into the existing `YourTurnEvent` SSE broadcast so `canDispose` reflects actual rule state.

## Capabilities

### New Capabilities

- `pawn-movement`: All pawn movement rules — board positions 1–64 (loop), home positions per player, finish area entry (4 slots), forward/backward movement, cannot-pass-protected-pawn constraint, hitting unprotected opponent pawns (returns pawn to reserve), teammate pawn control once own pawns are finished.
- `pawn-protection`: Protection lifecycle — pawn becomes protected when placed at home (Ace/King) or when it enters the finish area; loses protection on any move or Jack swap; protected pawns cannot be hit, passed, or swapped by other players.
- `playable-card-detection`: Per-card move enumeration and `HasPlayableCards` — Ace (enter or +1), King (enter), Four (−4 backward), Seven (one pawn 7 or split ≤2 pawns), Jack (swap two different-color non-finish pawns), Queen (+12), numbered cards (+face value); a card is playable if at least one legal move option exists.

### Modified Capabilities

## Impact

- `CardCheesi.Game.Abstractions`: `IPawn`, `IGameState` interface changes (new members).
- `CardCheesi.Game`: `Pawn`, `GameState` record implementations + new domain logic.
- `CardCheesi.Game.Tests`: New unit tests for each card type's move validation and protection rules.
- `CardCheesi.Game.Api` / SSE: `YourTurnEvent.canDispose` computed via `HasPlayableCards`.
