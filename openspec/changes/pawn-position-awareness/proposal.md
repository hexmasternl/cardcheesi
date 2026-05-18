## Why

The `PawnLayer` currently uses `placePawns()` to destroy and re-create every pawn mesh from scratch whenever game state changes — pawns teleport to their new positions with no animation. Before any pawn movement animation can be added, the game view must maintain a **per-pawn position map**: knowing the current world coordinates of each pawn and deriving the target world coordinates from the incoming `Pawn.location` so that the delta (old → new) can be handed to an animation system.

## What Changes

- Introduce a `PawnPositionMap` data structure (a `Map<pawnId, world XYZ>`) that tracks the last-known world position for every pawn on the board.
- Add a `resolveWorldPosition(pawn: Pawn, playerIndex: number, reserveIndex: number): Vector3` helper that derives the `Vector3` target for any pawn from its `PawnLocation` discriminated union, delegating to the existing `boardPositionToWorld`, `finishPositionToWorld`, and `RESERVE_POSITIONS` helpers in `board-coordinates.ts`.
- Extend `PawnLayer` with a `movePawns(players, status, blinking, selectable)` method that, instead of destroying and re-creating meshes, **re-uses existing `SpawnedPawn` entries** and updates their `root.position` (teleport for now; the position map makes animation easy to add in a follow-up).
- Replace the `placePawns()` call in `GameBoardComponent`'s `effect()` with `movePawns()` on subsequent calls (first placement still spawns fresh meshes).
- Export `resolveWorldPosition` from `board-coordinates.ts` so it can be used independently by future animation or SSE-driven update logic.

## Capabilities

### New Capabilities

- `pawn-position-map`: A per-pawn world-coordinate map maintained inside `PawnLayer`. Provides `resolveWorldPosition` to convert any `Pawn` + player context into a Babylon.js `Vector3`, and tracks the previously rendered position so callers can compute movement deltas.

### Modified Capabilities

- `game-board`: `GameBoardComponent` and `PawnLayer` are updated so that pawn position changes re-use existing mesh instances and update positions through the `PawnPositionMap` rather than full destroy-recreate cycles.

## Impact

- `src/App/src/app/pages/game/game-board/board-coordinates.ts` — new `resolveWorldPosition` export
- `src/App/src/app/pages/game/game-board/pawn-layer.ts` — `PawnPositionMap`, `movePawns()` method replacing `placePawns()` for incremental updates
- `src/App/src/app/pages/game/game-board/game-board.ts` — switch effect from `placePawns` to `movePawns` for non-initial renders
- No backend changes; no API changes; no new npm packages required
