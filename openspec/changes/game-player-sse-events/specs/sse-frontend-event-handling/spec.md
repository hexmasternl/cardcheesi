## ADDED Requirements

### Requirement: SseService handles player-joined event type
The frontend `SseService` SHALL subscribe to the `player-joined` SSE event type on the `EventSource` connection and expose the latest event payload as a typed Angular signal.

The signal SHALL be of type `PlayerJoinedEvent | null`, defaulting to `null`, and SHALL be updated each time a `player-joined` event is received.

#### Scenario: player-joined event is received
- **WHEN** the `EventSource` receives an SSE event with `event: player-joined`
- **THEN** the `lastPlayerJoined` signal is updated with the parsed `PlayerJoinedEvent` payload (`playerId`, `playerName`)

#### Scenario: Malformed player-joined payload is received
- **WHEN** the `EventSource` receives a `player-joined` event whose data is not valid JSON
- **THEN** the event is silently ignored and the signal value remains unchanged

### Requirement: SseService handles player-status event type
The frontend `SseService` SHALL subscribe to the `player-status` SSE event type and expose the latest event payload as a typed Angular signal of type `PlayerStatusEvent | null`.

#### Scenario: player-status event is received
- **WHEN** the `EventSource` receives an SSE event with `event: player-status`
- **THEN** the `lastPlayerStatus` signal is updated with the parsed payload (`playerId`, `playerName`, `status`)

### Requirement: GamePage reacts to player-joined events
`GamePage` SHALL react to the `lastPlayerJoined` signal from `SseService` and append the new player to its local `gameState` players array if the player is not already present.

#### Scenario: New player joins while game page is open
- **WHEN** a `player-joined` SSE event is received and the player is not already in the local `gameState.players` array
- **THEN** the new player is appended to `gameState.players` with the `id` and `name` from the event payload

#### Scenario: Duplicate player-joined event received
- **WHEN** a `player-joined` SSE event is received for a player whose `id` is already in `gameState.players`
- **THEN** the `gameState.players` array is NOT modified (deduplication by `id`)

#### Scenario: player-joined event received but gameState is null
- **WHEN** a `player-joined` SSE event is received while `gameState` is still `null`
- **THEN** the event is ignored and no error is thrown

### Requirement: PlayerPresenceStore seeds new player from player-joined event
`PlayerPresenceStore` SHALL react to the `lastPlayerJoined` signal from `SseService` and insert the new player into the presence map with status `Connected` if not already present.

#### Scenario: New player joins while presence panel is open
- **WHEN** a `player-joined` SSE event is received and the player is not yet in the presence map
- **THEN** a new entry is added to the presence map with `status: 'Connected'`

#### Scenario: player-joined event for an already-tracked player
- **WHEN** a `player-joined` SSE event is received for a player already in the presence map
- **THEN** the existing presence entry is NOT overwritten
