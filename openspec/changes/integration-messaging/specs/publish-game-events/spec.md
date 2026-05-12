## ADDED Requirements

### Requirement: Game API publishes GameCreatedEvent after a game is successfully created
The `CreateGameHandler` SHALL publish a `GameCreatedEvent` to the `"game-created"` Dapr topic immediately after the game is successfully persisted.

#### Scenario: Event published on game creation
- **WHEN** a new game is created successfully
- **THEN** a `GameCreatedEvent` SHALL be published containing `GameId`, `GameCode`, `CreatorPlayerId`, `CreatorPlayerName`, and `OccurredAt`

#### Scenario: No event published if game creation fails
- **WHEN** all unique code generation attempts are exhausted and a `DomainException` is thrown
- **THEN** no `GameCreatedEvent` SHALL be published

### Requirement: Game API publishes PlayerAddedToGameEvent when a player joins
The `JoinGameHandler` SHALL publish a `PlayerAddedToGameEvent` to the `"player-added-to-game"` Dapr topic immediately after the updated game (with the new player) is successfully persisted.

#### Scenario: Event published on player join
- **WHEN** a player successfully joins a game
- **THEN** a `PlayerAddedToGameEvent` SHALL be published containing `GameId`, `GameCode`, `PlayerId`, `PlayerName`, and `OccurredAt`

#### Scenario: No event published if join fails
- **WHEN** joining fails (game not found, full game, already joined, wrong status)
- **THEN** no `PlayerAddedToGameEvent` SHALL be published

### Requirement: Game API publishes PlayerLeftGameEvent when a player's grace period expires
The `PlayerPresenceTracker` SHALL publish a `PlayerLeftGameEvent` to the `"player-left-game"` Dapr topic when a player's status transitions from `Disconnected` to `Left` (i.e., after the 30-second grace period elapses without reconnection).

#### Scenario: Left event published after grace period
- **WHEN** a player disconnects and does not reconnect within 30 seconds
- **THEN** a `PlayerLeftGameEvent` SHALL be published containing `PlayerId`, `PlayerName`, `GameCode`, and `OccurredAt`

#### Scenario: No left event if player reconnects in time
- **WHEN** a player disconnects but reconnects before the grace period expires
- **THEN** no `PlayerLeftGameEvent` SHALL be published

### Requirement: Publish failure does not prevent the primary game action from succeeding
If `DaprClient.PublishEventAsync` throws an exception during game event publishing, the handler SHALL log the error and continue without re-throwing, so that the core game action is not rolled back.

#### Scenario: Publish failure is absorbed
- **WHEN** the Dapr sidecar is unavailable and `PublishEventAsync` throws during game event publishing
- **THEN** the game action (create/join) SHALL still succeed and the error SHALL be logged with level `Warning` or higher
