## ADDED Requirements

### Requirement: JoinGameHandler broadcasts player-joined SSE event
When a player successfully joins a game, the system SHALL broadcast a `player-joined` SSE event to all clients currently connected to that game's event stream.

The event payload SHALL be a JSON object with the fields:
- `playerId` (string, UUID)
- `playerName` (string)

The broadcast SHALL occur after the updated game state is persisted to the repository.

#### Scenario: Player joins a waiting game with connected observers
- **WHEN** a player successfully calls the JoinGame command for a game that has other players with open SSE connections
- **THEN** all connected clients receive an SSE event with `event: player-joined` and a JSON data payload containing the new player's `playerId` and `playerName`

#### Scenario: Player joins a game with no connected observers
- **WHEN** a player successfully calls the JoinGame command for a game where no other clients have open SSE connections
- **THEN** the broadcast completes without error and no SSE messages are sent

#### Scenario: JoinGame fails due to domain rule violation
- **WHEN** the JoinGame command fails (game full, already joined, not waiting)
- **THEN** no `player-joined` SSE event is broadcast

### Requirement: player-joined event payload schema
The `player-joined` SSE event data SHALL be a JSON-serialised object conforming to:

```json
{
  "playerId": "<uuid string>",
  "playerName": "<display name string>"
}
```

#### Scenario: Payload is valid JSON with required fields
- **WHEN** a `player-joined` SSE event is emitted
- **THEN** the `data` field parses as JSON and contains non-empty string values for both `playerId` and `playerName`
