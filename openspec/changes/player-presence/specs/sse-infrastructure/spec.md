## ADDED Requirements

### Requirement: SSE event stream endpoint
The system SHALL expose a `GET /games/{code}/events` endpoint that responds with `Content-Type: text/event-stream` and maintains a persistent HTTP connection, pushing named events to the client as they occur.

#### Scenario: Client subscribes to a valid game
- **WHEN** a client sends `GET /games/{code}/events?playerId={id}` for an existing game
- **THEN** the server responds with HTTP 200, `Content-Type: text/event-stream`, and holds the connection open

#### Scenario: Client subscribes to an unknown game
- **WHEN** a client sends `GET /games/{code}/events` with a code that does not match any game
- **THEN** the server responds with HTTP 404 and closes the connection

#### Scenario: Multiple clients subscribe to the same game
- **WHEN** two or more clients open the SSE stream for the same game code
- **THEN** each client receives all events broadcast to that game independently

#### Scenario: Client disconnects
- **WHEN** a subscribed client closes the connection (tab closed, network loss)
- **THEN** the server removes the client's channel and stops writing to it without error

### Requirement: Typed event envelope
The system SHALL send events using the SSE named-event format so that clients can listen for specific event types without inspecting payload fields.

#### Scenario: Event is broadcast to subscribers
- **WHEN** the server broadcasts an event of type `player-status`
- **THEN** each connected client receives a frame with `event: player-status` followed by `data: <JSON payload>`

### Requirement: Server keep-alive
The system SHALL send a SSE comment (`: keep-alive`) to all subscribers of a game every 15 seconds to prevent intermediary proxies from closing idle connections.

#### Scenario: Keep-alive emitted on idle connection
- **WHEN** no events have been sent to a subscriber for 15 seconds
- **THEN** the server writes a `: keep-alive` comment line to the response stream
