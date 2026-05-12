## ADDED Requirements

### Requirement: Player can register with a display name
The system SHALL expose a `POST /players` endpoint that accepts a player display name, persists a new `PlayerEntity` record to PostgreSQL, and returns a signed JWT identity token. No password or prior account is required.

#### Scenario: Successful registration returns access token and sets refresh cookie
- **WHEN** a client sends `POST /players` with body `{ "name": "Alice" }`
- **THEN** the system responds `201 Created` with body `{ "token": "<signed-jwt>" }` where the JWT `sub` claim equals the newly generated player GUID and the `name` claim equals `"Alice"`, AND the response sets a `Set-Cookie` header named `cc_refresh` with `HttpOnly`, `Secure`, `SameSite=Strict`, `Path=/players/refresh`, and `Max-Age=2592000` attributes

#### Scenario: Player record is persisted with LastSeenAt
- **WHEN** a successful `POST /players` request completes
- **THEN** a row exists in the `Players` table with the GUID matching the JWT `sub`, the supplied name, a non-null `CreatedAt` timestamp, and a `LastSeenAt` value equal to `CreatedAt` (± 1 second)

### Requirement: Player name SHALL be validated before registration
The system SHALL reject any registration request where the player name is empty, exceeds 50 characters, or contains leading/trailing whitespace or ASCII control characters.

#### Scenario: Empty name is rejected
- **WHEN** a client sends `POST /players` with body `{ "name": "" }`
- **THEN** the system responds `400 Bad Request` with a `ValidationProblemDetails` body identifying the `name` field

#### Scenario: Name exceeding 50 characters is rejected
- **WHEN** a client sends `POST /players` with a name of 51 characters
- **THEN** the system responds `400 Bad Request` with a `ValidationProblemDetails` body identifying the `name` field

#### Scenario: Name with leading whitespace is rejected
- **WHEN** a client sends `POST /players` with body `{ "name": "  Bob" }`
- **THEN** the system responds `400 Bad Request` with a `ValidationProblemDetails` body identifying the `name` field

#### Scenario: Name at maximum allowed length is accepted
- **WHEN** a client sends `POST /players` with a name of exactly 50 non-whitespace characters
- **THEN** the system responds `201 Created` with a valid JWT

### Requirement: Registration endpoint SHALL not expose internal errors to the caller
The system SHALL return `500 Internal Server Error` with a generic `ProblemDetails` body (no stack traces, no DB error messages) when an unexpected error occurs during registration.

#### Scenario: Database unavailable during registration
- **WHEN** the PostgreSQL server is unreachable and a client sends `POST /players`
- **THEN** the system responds `500 Internal Server Error` with a generic error body containing no database-specific details
