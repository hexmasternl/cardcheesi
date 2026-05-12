## MODIFIED Requirements

### Requirement: Authenticated player creates a new game
The system SHALL expose a `POST /games` endpoint that requires a valid Bearer JWT. The creating player's identity SHALL be derived from the JWT `sub` (GUID) and `name` claims. The request body SHALL NOT contain a player name field.

#### Scenario: Authenticated player successfully creates a game
- **WHEN** an authenticated player sends `POST /games` with a valid Bearer token and an empty JSON body `{}`
- **THEN** the system responds `200 OK` with body `{ "gameId": "<guid>", "gameCode": "<6-char-code>" }` and the game record contains the authenticated player as the first participant

#### Scenario: Unauthenticated request to create a game is rejected
- **WHEN** a client sends `POST /games` without an `Authorization` header
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Game code is unique alphanumeric 6-character string
- **WHEN** a game is successfully created
- **THEN** the returned `gameCode` consists of exactly 6 uppercase alphanumeric characters (A–Z, 2–9, excluding ambiguous characters I, O, 1, 0)

#### Scenario: Collision retry produces a unique code
- **WHEN** all attempted game codes conflict with existing records (up to 5 attempts)
- **THEN** the system responds `503 Service Unavailable` with a descriptive error message

### Requirement: Authenticated player joins an existing game
The system SHALL expose a `POST /games/{code}/join` endpoint that requires a valid Bearer JWT. The joining player's identity SHALL be derived from the JWT claims. The request body SHALL NOT contain a player name field.

#### Scenario: Authenticated player successfully joins a game
- **WHEN** an authenticated player sends `POST /games/{code}/join` with a valid Bearer token and an empty JSON body `{}`
- **THEN** the system responds `200 OK` with body `{ "gameId": "<guid>", "playerId": "<jwt-sub-guid>", "gameCode": "<code>" }` and the player is added to the game

#### Scenario: Joining a non-existent game returns 404
- **WHEN** an authenticated player sends `POST /games/{code}/join` where `{code}` does not match any game
- **THEN** the system responds `404 Not Found` with a descriptive error body

#### Scenario: Unauthenticated request to join a game is rejected
- **WHEN** a client sends `POST /games/{code}/join` without an `Authorization` header
- **THEN** the system responds `401 Unauthorized`
