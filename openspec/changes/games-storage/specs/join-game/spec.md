## MODIFIED Requirements

### Requirement: Join game reads and writes through IGameRepository
The `POST /games/{code}/join` endpoint SHALL use `IGameRepository.GetByCodeAsync` to look up the game and `IGameRepository.SaveAsync` to persist the updated `GameState` after the player is added. Direct `DbContext` access SHALL NOT be used in the join flow.

#### Scenario: Player joins — state updated via repository
- **WHEN** an authenticated player calls `POST /games/{code}/join` with a valid code for a waiting game
- **THEN** `IGameRepository.GetByCodeAsync` is called with the provided code
- **THEN** the player is added to the `GameState`'s participant list
- **THEN** `IGameRepository.SaveAsync` is called with the updated `GameState`
- **THEN** the updated `State` JSONB in the database reflects the new participant

#### Scenario: Game not found returns 404
- **WHEN** `IGameRepository.GetByCodeAsync` returns `null` for the provided code
- **THEN** the endpoint returns HTTP 404 with a descriptive error message

#### Scenario: Concurrent join does not corrupt state
- **WHEN** two players attempt to join the same game simultaneously
- **THEN** only one succeeds in adding their entry; the final `State` JSONB contains both players without duplication or data loss (last-write-wins is acceptable at this stage)
