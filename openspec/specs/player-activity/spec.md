## Purpose

Define player activity tracking, cleanup, and silent session restore behavior.

## Requirements

### Requirement: Player LastSeenAt SHALL be updated on every successful token refresh
The system SHALL update the `LastSeenAt` column on the `Players` row to the current UTC timestamp each time a `POST /players/refresh` succeeds.

#### Scenario: LastSeenAt updated after refresh
- **WHEN** a client performs a successful `POST /players/refresh`
- **THEN** the player's `LastSeenAt` in the database equals the timestamp of the refresh request (tolerance ± 2 seconds)

#### Scenario: LastSeenAt not updated on failed refresh
- **WHEN** a client sends `POST /players/refresh` with an invalid or expired cookie
- **THEN** the player's `LastSeenAt` in the database is unchanged

### Requirement: The cleanup service SHALL remove players inactive for 31 days or longer
The system SHALL run a background cleanup once per day. During each run it SHALL:
1. Delete all `RefreshTokens` rows whose `ExpiresAt` is in the past.
2. Delete all `Players` rows (and their associated `RefreshTokens` via cascade) whose `LastSeenAt` is older than 31 days from the time the sweep runs.

#### Scenario: Inactive player is deleted
- **WHEN** the cleanup service runs and a player's `LastSeenAt` is 31 days or more in the past
- **THEN** that player's row and all associated `RefreshTokens` rows are removed from the database

#### Scenario: Recently active player is not deleted
- **WHEN** the cleanup service runs and a player's `LastSeenAt` is 30 days or fewer in the past
- **THEN** that player's row is NOT removed from the database

#### Scenario: Expired refresh tokens are removed independently
- **WHEN** the cleanup service runs and a `RefreshTokens` row has `ExpiresAt` in the past but the associated player has been active within 31 days
- **THEN** only the expired `RefreshTokens` row is deleted; the player row is retained

#### Scenario: Cleanup does not fail when no rows match
- **WHEN** the cleanup service runs and there are no inactive players or expired tokens
- **THEN** the service completes without error and logs that 0 players and 0 tokens were removed

### Requirement: The frontend SHALL silently restore the player session using the refresh token on app load
When the Angular application initialises, if the `cc_refresh` cookie is present, the app SHALL call `POST /players/refresh` before rendering protected routes. If the refresh succeeds, the returned access token is stored in memory and the player is considered logged in. If the refresh fails (e.g., cookie expired or revoked), the player is directed to the registration screen.

#### Scenario: Successful silent restore on app load
- **WHEN** the Angular app initialises and the `cc_refresh` cookie exists and is valid
- **THEN** the app calls `POST /players/refresh`, stores the returned access token in memory, and proceeds to the main game screen without prompting the player to register

#### Scenario: Expired or missing cookie redirects to registration
- **WHEN** the Angular app initialises and there is no `cc_refresh` cookie, or the refresh call returns `401`
- **THEN** the app displays the registration screen and does not attempt to navigate to a protected route
