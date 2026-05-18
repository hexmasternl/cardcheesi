## MODIFIED Requirements

### Requirement: Pawn positions are updated reactively from game state
`GameBoardComponent` SHALL call `movePawns()` (instead of `placePawns()`) when the `players` or `gameStatus` input signals change after the initial placement, so that existing pawn meshes are repositioned in place rather than destroyed and re-created.

#### Scenario: Initial render spawns all pawns fresh
- **WHEN** the Babylon.js scene is initialised for the first time
- **THEN** `placePawns()` is called once to spawn all pawn meshes from scratch

#### Scenario: Subsequent state change repositions pawns in place
- **WHEN** the `players` signal emits an updated value after the initial render
- **THEN** `movePawns()` is called (not `placePawns()`)
- **THEN** pawn meshes that already exist are repositioned via `root.position` updates

#### Scenario: Highlight/blink behaviour is unchanged
- **WHEN** `movePawns()` is called
- **THEN** `updateHighlights(blinking, selectable)` is applied using the same logic as after `placePawns()`
