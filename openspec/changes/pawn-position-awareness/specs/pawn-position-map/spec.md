## ADDED Requirements

### Requirement: `resolveWorldPosition` converts any pawn location to a world coordinate
`board-coordinates.ts` SHALL export a `resolveWorldPosition(pawn: Pawn, playerIndex: number, reserveIndex: number): Vector3` function that maps a pawn's `PawnLocation` discriminated union (`reserve`, `board`, or `finish`) to the corresponding Babylon.js `Vector3` world position using the existing `boardPositionToWorld`, `finishPositionToWorld`, and `RESERVE_POSITIONS` helpers.

#### Scenario: Reserve pawn resolves to the correct reserve slot
- **WHEN** `resolveWorldPosition` is called with a pawn whose `location.$type === 'reserve'` and `reserveIndex = 2` for player index 1
- **THEN** the returned `Vector3` matches `RESERVE_POSITIONS[1][2]` (x, y, z)

#### Scenario: Board pawn resolves to the correct board position
- **WHEN** `resolveWorldPosition` is called with a pawn whose `location.$type === 'board'` and `location.position = 17`
- **THEN** the returned `Vector3` matches `boardPositionToWorld(17)`

#### Scenario: Finish pawn resolves to the correct finish slot
- **WHEN** `resolveWorldPosition` is called with a pawn whose `location.$type === 'finish'` and `location.slot = 2` for player index 0
- **THEN** the returned `Vector3` matches `finishPositionToWorld(0, 2)`

---

### Requirement: `PawnLayer` maintains a `pawnPositionMap` keyed by pawn ID
`PawnLayer` SHALL maintain a `Map<string, Vector3>` (the `pawnPositionMap`) that is updated every time a pawn's world position changes. Each entry records the last rendered world position for that pawn.

#### Scenario: Map is populated after initial placement
- **WHEN** `placePawns()` completes for a game with 2 players, each with 4 pawns
- **THEN** `pawnPositionMap` contains exactly 8 entries
- **THEN** each entry's `Vector3` equals the world position the corresponding pawn mesh was placed at

#### Scenario: Map is updated when a pawn moves
- **WHEN** `movePawns()` is called with an updated game state where pawn `p1` has moved from board position 5 to board position 9
- **THEN** `pawnPositionMap.get('p1')` equals `boardPositionToWorld(9)` after the call

#### Scenario: Map entry is removed when pawn is no longer present
- **WHEN** `movePawns()` is called and a pawn ID that was previously in the map no longer exists in the updated player list
- **THEN** `pawnPositionMap` does NOT contain an entry for that pawn ID

---

### Requirement: `PawnLayer.movePawns()` re-uses existing mesh instances
`PawnLayer` SHALL expose a `movePawns(players, status, blinking, selectable)` method with the same signature as `placePawns()`. For any pawn ID already tracked in the internal mesh registry, `movePawns()` SHALL update `root.position` in place rather than disposing and re-instantiating the mesh. New pawn IDs SHALL spawn fresh mesh instances. Pawn IDs absent from the updated state SHALL have their meshes disposed.

#### Scenario: Existing pawn mesh is repositioned, not re-created
- **WHEN** `movePawns()` is called and pawn `p1` was previously spawned
- **THEN** the same `SpawnedPawn.root` object reference is used (no dispose/re-instantiate)
- **THEN** `root.position` equals `resolveWorldPosition(pawn, playerIndex, reserveIndex)` for pawn `p1`

#### Scenario: New pawn is spawned on first appearance
- **WHEN** `movePawns()` is called and pawn `p99` was not previously tracked
- **THEN** a new `SpawnedPawn` entry is added to the registry for `p99`

#### Scenario: Stale pawn mesh is disposed
- **WHEN** `movePawns()` is called and pawn `p42` no longer appears in the player list
- **THEN** `p42`'s mesh root is disposed
- **THEN** `p42` is removed from both the registry and `pawnPositionMap`
