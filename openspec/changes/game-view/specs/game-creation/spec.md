## ADDED Requirements

### Requirement: Game state is retrievable by code
The API SHALL expose a `GET /games/{code}` endpoint that returns the full `GameState` for the given game code with HTTP 200. If no game with that code exists, the endpoint SHALL return HTTP 404.

#### Scenario: Existing game is returned
- **WHEN** a client calls `GET /games/ABC123`
- **THEN** the server returns HTTP 200 with the `GameState` JSON body for that game code

#### Scenario: Non-existent game returns 404
- **WHEN** a client calls `GET /games/XXXXXX` where no game with that code exists
- **THEN** the server returns HTTP 404 with no body
