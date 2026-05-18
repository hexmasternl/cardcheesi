## 1. `board-coordinates.ts` — Position Resolution Helper

- [ ] 1.1 Add `resolveWorldPosition(pawn: Pawn, playerIndex: number, reserveIndex: number): Vector3` function to `board-coordinates.ts` that maps `PawnLocation` to a Babylon.js `Vector3` via the existing helpers (`boardPositionToWorld`, `finishPositionToWorld`, `RESERVE_POSITIONS`)
- [ ] 1.2 Export `resolveWorldPosition` from `board-coordinates.ts`

## 2. `PawnLayer` — Mesh Registry and Position Map

- [ ] 2.1 Change the internal `spawnedPawns: SpawnedPawn[]` array to `spawnedPawns: Map<string, SpawnedPawn>` keyed by `pawnId` for O(1) lookup (update all existing usages: `placePawns`, `updateHighlights`, `tickBlink`, `dispose`)
- [ ] 2.2 Add `readonly pawnPositionMap: Map<string, Vector3>` property to `PawnLayer`
- [ ] 2.3 Update `placePawns()` to write each pawn's resolved `Vector3` into `pawnPositionMap` after spawning, and clear the map before re-spawning
- [ ] 2.4 Add `movePawns(players: GamePlayer[], status: 0|1|2, blinking: string[], selectable: string[]): void` method that:
  - Determines all pawn IDs present in the new state
  - Disposes and removes map entries for stale pawn IDs (present in `spawnedPawns` but not in new state)
  - For each pawn in the new state: if already in `spawnedPawns`, compute target `Vector3` via `resolveWorldPosition` and update `root.position` and `pawnPositionMap`; otherwise spawn a new mesh and record in map
  - Calls `updateHighlights(blinking, selectable)` after all positions are updated

## 3. `GameBoardComponent` — Switch to `movePawns` for Incremental Updates

- [ ] 3.1 Track whether the initial placement has occurred (e.g. a `private initialised = false` flag set to `true` after the first `initScene` call completes and calls `placePawns()`)
- [ ] 3.2 Update the reactive `effect()` in `GameBoardComponent` to call `this.pawnLayer?.movePawns(...)` instead of `this.pawnLayer?.placePawns(...)`, so that subsequent state changes reuse existing meshes

## 4. Unit Tests

- [ ] 4.1 Add unit tests for `resolveWorldPosition` in a new `board-coordinates.spec.ts` file — cover all three `PawnLocation` types (`reserve`, `board`, `finish`)
- [ ] 4.2 Add tests for `PawnLayer.movePawns()` verifying: existing pawn root is reused (same object reference), new pawn is spawned, stale pawn is disposed, `pawnPositionMap` is updated correctly after each call
