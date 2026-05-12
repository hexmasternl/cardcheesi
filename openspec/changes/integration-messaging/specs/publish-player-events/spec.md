## ADDED Requirements

### Requirement: Players API publishes PlayerCreatedEvent after successful registration
The `RegisterPlayerHandler` SHALL publish a `PlayerCreatedEvent` to the `"player-created"` Dapr topic immediately after a player is successfully persisted to the database.

#### Scenario: Event published on successful registration
- **WHEN** a new player is registered successfully
- **THEN** a `PlayerCreatedEvent` SHALL be published containing the new player's `PlayerId`, `PlayerName`, and `OccurredAt`

#### Scenario: No event published if registration fails
- **WHEN** registration fails (e.g., database error)
- **THEN** no `PlayerCreatedEvent` SHALL be published

### Requirement: Game API publishes PlayerWentOfflineEvent when a player disconnects
The `PlayerPresenceTracker` SHALL publish a `PlayerWentOfflineEvent` to the `"player-went-offline"` Dapr topic when a player's SSE connection is lost and their status transitions to `Disconnected`.

#### Scenario: Offline event published on disconnect
- **WHEN** a player's SSE connection drops
- **THEN** a `PlayerWentOfflineEvent` SHALL be published containing `PlayerId`, `PlayerName`, `GameCode`, and `OccurredAt`

### Requirement: Game API publishes PlayerCameOnlineEvent when a player reconnects
The `PlayerPresenceTracker` SHALL publish a `PlayerCameOnlineEvent` to the `"player-came-online"` Dapr topic when a player's SSE connection is established or re-established.

#### Scenario: Online event published on connect
- **WHEN** a player's SSE connection is established
- **THEN** a `PlayerCameOnlineEvent` SHALL be published containing `PlayerId`, `PlayerName`, `GameCode`, and `OccurredAt`

### Requirement: Publish failure does not prevent the primary action from succeeding
If `DaprClient.PublishEventAsync` throws an exception, the handler SHALL log the error and continue without re-throwing, so that the core player/game action is not rolled back due to a messaging failure.

#### Scenario: Publish failure is absorbed
- **WHEN** the Dapr sidecar is unavailable and `PublishEventAsync` throws
- **THEN** the registration (or presence update) SHALL still succeed and the error SHALL be logged with level `Warning` or higher
