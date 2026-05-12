## ADDED Requirements

### Requirement: Player connection tracking
The server SHALL maintain an in-memory presence state for each player in a game. Valid states are `Connected`, `Disconnected`, and `Left`.

#### Scenario: Player opens the game page
- **WHEN** a client opens `GET /games/{code}/events?playerId={id}` for a player in that game
- **THEN** the server sets that player's status to `Connected` and broadcasts a `player-status` event to all subscribers

#### Scenario: Player's SSE connection drops
- **WHEN** an active SSE connection for a player is closed by the client
- **THEN** the server sets that player's status to `Disconnected` and broadcasts a `player-status` event to all remaining subscribers

#### Scenario: Player reconnects within the grace period
- **WHEN** a player's status is `Disconnected` and the player re-opens the SSE stream before 30 seconds elapse
- **THEN** the server cancels the grace timer, sets the player's status to `Connected`, and broadcasts a `player-status` event

#### Scenario: Grace period expires without reconnection
- **WHEN** a player's status is `Disconnected` and 30 seconds pass without the player reconnecting
- **THEN** the server sets the player's status to `Left` and broadcasts a final `player-status` event to all remaining subscribers

### Requirement: Player status event payload
Each `player-status` SSE event SHALL carry a JSON payload identifying the player and their new status so clients can update the display without re-fetching the full game state.

#### Scenario: Status event payload structure
- **WHEN** a `player-status` event is broadcast
- **THEN** the `data` field is a JSON object with fields `playerId` (string/GUID), `playerName` (string), and `status` (one of `Connected`, `Disconnected`, `Left`)

### Requirement: Initial presence snapshot on subscribe
When a client subscribes to the SSE stream, the server SHALL immediately send the current presence status of all players in the game so the panel is populated without waiting for the next change event.

#### Scenario: Snapshot sent on connection open
- **WHEN** a client opens the SSE stream for a game with N players
- **THEN** the server sends N `player-status` events (one per player) before any other events

### Requirement: Player presence panel
The frontend SHALL display a fixed overlay panel in the top-left corner of the game page listing all players with a colour-coded status indicator.

#### Scenario: Panel shows all players on game load
- **WHEN** the game page loads and the SSE stream connects
- **THEN** the panel lists every player in the game with their name and current status indicator

#### Scenario: Status indicator updates in real-time
- **WHEN** a `player-status` SSE event is received
- **THEN** the corresponding player's status indicator in the panel updates immediately without a page reload

#### Scenario: Status indicator colours
- **WHEN** a player's status is `Connected`
- **THEN** the indicator is green

#### Scenario: Disconnected indicator
- **WHEN** a player's status is `Disconnected`
- **THEN** the indicator is yellow/amber

#### Scenario: Left indicator
- **WHEN** a player's status is `Left`
- **THEN** the indicator is red
