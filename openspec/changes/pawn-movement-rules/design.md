## Context

The `GameState` record in `CardCheesi.Game` has two domain methods that are `NotImplementedException` stubs: `PlayCard` and `MakeMove`. The board layout is 64 board positions (1–64, looping) plus 4 personal finish slots per player. Player home positions are fixed: P1→1, P2→17, P3→33, P4→49. The current `IPawn` model has no `IsProtected` field. The `YourTurnEvent` SSE message already carries `canDispose` but it is not yet computed from game state.

## Goals / Non-Goals

**Goals:**
- Implement all pawn movement rules and invariants inside the domain model (pure, immutable record updates).
- Add `IPawn.IsProtected` to capture the protection lifecycle.
- Add `IGameState.HasPlayableCards(Guid playerId)` for dispose detection.
- Add `IGameState.GetValidMoves(Guid playerId, Card card)` for move enumeration.
- Implement `GameState.MakeMove` and `GameState.PlayCard` with full rule enforcement.
- Keep all domain logic in `CardCheesi.Game` / `CardCheesi.Game.Abstractions`; no API-layer changes except wiring `canDispose` correctly.

**Non-Goals:**
- Networked card-play API endpoint (no new HTTP endpoints in this change).
- Persistence of individual moves (game state is stored as a whole).
- Frontend rendering of valid-move hints.

## Decisions

### D1 — Immutable domain model (records, no mutation)
All operations return a new `GameState` instance via `with { }`. This keeps domain logic pure and testable without fakes or mocks.

*Alternative considered*: Mutable entities (domain events). Rejected — record-based updates are consistent with the existing codebase style and simpler for this phase.

### D2 — Dedicated `MoveOption` value objects per card type
Rather than overloading `MakeMove(Guid pawnId, int spaces)`, we add parallel typed overloads on the concrete `GameState`:
- `MakeMove(Guid pawnId, int spaces)` — single pawn forward/backward (Ace, Two–Ten, Queen, Four).
- `SwapPawns(Guid pawnId1, Guid pawnId2)` — Jack swap.
- `MakeSplitMove(Guid pawnId1, int spaces1, Guid? pawnId2, int spaces2)` — Seven split (pawnId2 null = single pawn move).

The `IGameState` interface gains corresponding method signatures. **BREAKING**: `IGameState.MakeMove` is supplemented by `SwapPawns` and `MakeSplitMove`.

*Alternative*: A single polymorphic `Move` command object. Rejected — adds indirection without benefit at this stage.

### D3 — Protection tracked on the Pawn record
`Pawn` gains `bool IsProtected`. Protection is set to `true` on entering home (Ace/King) and on entering the finish area; cleared to `false` on any subsequent move or swap. This makes protection state visible to the frontend and queryable by validation logic without scanning game state.

### D4 — Board arithmetic helpers in a static `BoardRules` class
A new `CardCheesi.Game.Rules.BoardRules` static class encapsulates:
- `HomePosition(int playerIndex)` → 1 / 17 / 33 / 49
- `FinishEntryPosition(int playerIndex)` → position just before the finish corridor
- `Advance(int from, int steps, int playerIndex, IReadOnlyList<Pawn> allPawns)` → returns `PawnLocation` or throws if blocked
- `CanPass(int throughPosition, IReadOnlyList<Pawn> allPawns)` → protected-pawn check

Centralising these in `BoardRules` keeps `GameState` readable and makes the rules independently testable.

### D5 — `GetValidMoves` returns a discriminated result type
`GetValidMoves(Guid playerId, Card card)` returns `IReadOnlyList<MoveOption>` where `MoveOption` is an abstract record with concrete subtypes (`SingleMove`, `SplitMove`, `SwapMove`). An empty list means the card cannot be played.

## Risks / Trade-offs

- [Split Seven complexity] → Enumerate all valid (p1,s1,p2,s2) pairs where s1+s2=7 and both sub-moves are individually legal. The number of pairs is small (≤ O(pawns² × 7)) so brute-force enumeration is fine.
- [Finish area wrap-around] → A pawn on the main loop approaching its finish must detect when the advance "enters" the finish corridor mid-step. `BoardRules.Advance` handles this with player-index-aware branching.
- [Teammate pawns] → `HasPlayableCards` must check whether the player's own pawns are all finished and, if so, iterate teammate pawns instead. This requires knowing team membership, passed via `GameState.Teams`.
