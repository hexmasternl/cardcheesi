## MODIFIED Requirements

### Requirement: Game is persisted on creation
When a new game is created via `POST /games`, the server SHALL persist the initial `GameState` by calling `IGameRepository.SaveAsync`. The `GameCode` SHALL be stored as the dedicated `GameCode` column; the full `GameState` (including `Id`, participants, status, and domain state) SHALL be serialized to the `State` JSONB column.

#### Scenario: New game saved to database
- **WHEN** an authenticated player calls `POST /games`
- **THEN** `IGameRepository.SaveAsync` is called once with the newly constructed `GameState`
- **THEN** a row appears in the `Games` table with the correct `GameCode` and a non-null `State`

#### Scenario: Game code uniqueness enforced on creation
- **WHEN** a generated game code collides with an existing `GameCode` in the database
- **THEN** the server regenerates a new code and retries until a unique code is found before saving

#### Scenario: Created game has Waiting status
- **WHEN** a new game is created
- **THEN** the `State` JSONB contains a `Status` value of `Waiting`
- **THEN** the `State` JSONB contains exactly one participant (the creating player)
