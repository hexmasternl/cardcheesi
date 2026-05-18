## Context

`PawnLayer` in `src/App/.../game-board/pawn-layer.ts` is the sole owner of Babylon.js pawn meshes. Its current `placePawns()` method disposes every `SpawnedPawn` and re-instantiates from scratch on each call — a stateless, teleport-only approach. The coordinate-resolution helpers (`boardPositionToWorld`, `finishPositionToWorld`, `RESERVE_POSITIONS`) already exist in `board-coordinates.ts` and are correct. What is missing is:

1. A stable **per-pawn mesh registry** keyed by `pawnId` so the same mesh can be reused across state changes.
2. A **`resolveWorldPosition`** function that turns any `PawnLocation` + player context into a `Vector3` — usable by both the layer internals and any future animation / SSE-driven caller.
3. A **`PawnPositionMap`** (`Map<string, Vector3>`) that records the last rendered position, enabling future callers to compute the movement delta.

`GameBoardComponent` wires everything through Angular `effect()` inputs; it calls `placePawns()` whenever any input signal changes. This effect will be updated to call a new incremental `movePawns()` instead.

## Goals / Non-Goals

**Goals:**
- Export `resolveWorldPosition(pawn, playerIndex, reserveIndex)` from `board-coordinates.ts`.
- Maintain a `pawnPositionMap: Map<string, Vector3>` in `PawnLayer` that is updated on every placement/move.
- Add `movePawns()` to `PawnLayer` that re-uses existing mesh roots when a pawn's `pawnId` is already spawned, only spawning new meshes for genuinely new pawns and disposing meshes for pawns that have left.
- Update `GameBoardComponent` to call `movePawns()` in the reactive effect (replacing the first `placePawns()` call that already uses the initial input values) once the scene is ready.
- All existing highlight/blink behaviour is preserved unchanged.

**Non-Goals:**
- Animated movement along a path (a follow-up concern; this change provides the position map as the prerequisite).
- Backend changes or new API calls.
- Any changes to the SSE layer or `GamePage`.

## Decisions

### D1 — `resolveWorldPosition` as a pure function in `board-coordinates.ts`
Reserve index is a caller concern; the function receives it rather than computing it internally. This keeps the function stateless and easily unit-testable. The existing three helpers remain unchanged as lower-level primitives.

*Alternative*: Move resolution logic into `PawnLayer`. Rejected — `board-coordinates.ts` is the canonical mapping layer; future consumers (SSE handler, animation service) benefit from importing it directly without depending on `PawnLayer`.

### D2 — `pawnPositionMap` stored on `PawnLayer`, not in Angular state
The map lives alongside the meshes. It is updated synchronously in `movePawns()` immediately after `root.position` changes. No Angular signal or `Subject` is needed; callers that want to animate can read the map's previous value before calling `movePawns()`.

*Alternative*: Store in a `signal<Map<string, Vector3>>` on `GameBoardComponent`. Rejected — creates unnecessary coupling between Angular reactive state and Babylon.js internals; the map has no reason to trigger Angular change detection.

### D3 — `movePawns()` replaces `placePawns()` for incremental updates; `placePawns()` is kept for the initial spawn
On first render (meshes not yet spawned) the existing `placePawns()` path still applies. `movePawns()` is called on subsequent state changes. This avoids a "pawn missing during first render" race.

*Alternative*: Merge both into one method. Feasible, but complicates the branching logic. Keeping them separate makes the intent explicit.

### D4 — Reserve index re-calculated by iterating `player.pawns` in order
Reserve spots fill left-to-right as pawns are encountered with `$type === 'reserve'`. This matches the current `placePawns()` behaviour and the `RESERVE_POSITIONS` array layout.

## Risks / Trade-offs

- [SpawnedPawn keying] `spawnedPawns` is currently an array; it will be changed to a `Map<string, SpawnedPawn>` for O(1) lookup by `pawnId`. This is a minor internal refactor with no external API change.
- [Reserve index stability] If reserve pawn order in `player.pawns` changes between renders, the physical reserve spot may shift. Acceptable — reserve display is cosmetic and all four slots appear identical.
- [Memory leaks] Pawns removed from the game state (sent to finish) must be explicitly disposed. `movePawns()` must compare new pawn IDs against the map and dispose stale entries.
