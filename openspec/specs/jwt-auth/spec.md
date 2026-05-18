## Purpose

Define JWT issuance, validation, refresh, revocation, and cookie security behavior.

## Requirements

### Requirement: The API SHALL sign JWTs using HMAC-SHA256 with a configured symmetric key
The system SHALL use the HS256 algorithm to sign all issued JWTs. The signing key SHALL be sourced from configuration key `Jwt__SigningKey`. The system SHALL refuse to start if the key is absent or shorter than 32 bytes.

#### Scenario: Server starts with a valid signing key
- **WHEN** the `Jwt__SigningKey` configuration value is set to a 32-byte or longer value
- **THEN** the server starts successfully and is able to sign and validate tokens

#### Scenario: Server refuses to start with a missing signing key
- **WHEN** the `Jwt__SigningKey` configuration value is absent
- **THEN** the application fails to start and logs a clear error message indicating that `Jwt__SigningKey` is required

#### Scenario: Server refuses to start with a short signing key
- **WHEN** the `Jwt__SigningKey` value is present but shorter than 32 bytes
- **THEN** the application fails to start and logs a clear error message indicating the minimum key length requirement

### Requirement: Issued JWTs SHALL contain the required claims
Every JWT issued by `POST /players` SHALL contain: `sub` (player GUID), `name` (display name), `iss` (configured issuer), `aud` (configured audience), `iat` (issued-at), and `exp` (expiry).

#### Scenario: JWT contains all required claims
- **WHEN** a successful `POST /players` response is received
- **THEN** the decoded JWT header indicates algorithm `HS256` and the payload contains `sub`, `name`, `iss`, `aud`, `iat`, and `exp` claims with non-null values

#### Scenario: JWT expires after 10 minutes
- **WHEN** a JWT is issued with the default configuration
- **THEN** the `exp` claim is exactly 10 minutes after the `iat` claim (tolerance ± 5 seconds)

### Requirement: Protected endpoints SHALL reject requests without a valid Bearer token
Endpoints decorated as requiring authentication SHALL return `401 Unauthorized` when the `Authorization` header is absent, malformed, or carries an expired or tampered token.

#### Scenario: Missing Authorization header
- **WHEN** a client calls a protected endpoint without an `Authorization` header
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Expired token is rejected
- **WHEN** a client calls a protected endpoint with a token whose `exp` claim is in the past
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Token signed with a different key is rejected
- **WHEN** a client calls a protected endpoint with a token signed by a key other than `Jwt__SigningKey`
- **THEN** the system responds `401 Unauthorized`

#### Scenario: Valid token is accepted
- **WHEN** a client calls a protected endpoint with a valid, non-expired Bearer token issued by `POST /players`
- **THEN** the system processes the request normally and the handler can read the `sub` and `name` claims from `HttpContext.User`

### Requirement: JWT issuer and audience SHALL be configurable
The system SHALL read `Jwt__Issuer` and `Jwt__Audience` from configuration, defaulting to `cardcheesi-api` for both when not explicitly set.

#### Scenario: Default issuer and audience
- **WHEN** neither `Jwt__Issuer` nor `Jwt__Audience` are set in configuration
- **THEN** issued JWTs carry `iss: "cardcheesi-api"` and `aud: "cardcheesi-api"`

#### Scenario: Custom issuer is reflected in issued tokens
- **WHEN** `Jwt__Issuer` is set to `"my-issuer"` and a player registers
- **THEN** the issued JWT carries `iss: "my-issuer"`

### Requirement: A valid refresh token SHALL issue a new access token and rotate the refresh token
The system SHALL expose a `POST /players/refresh` endpoint. When a request arrives with a valid, non-revoked, non-expired `cc_refresh` cookie, the system SHALL issue a new short-lived access token, revoke the presented refresh token (`RevokedAt` set), issue a new refresh token, and set a new `cc_refresh` cookie.

#### Scenario: Successful token refresh
- **WHEN** a client sends `POST /players/refresh` with a valid `cc_refresh` cookie
- **THEN** the system responds `200 OK` with body `{ "token": "<new-access-jwt>" }` and sets a new `cc_refresh` cookie containing a different refresh token value

#### Scenario: Refresh token is rotated on use
- **WHEN** a successful refresh occurs
- **THEN** the previously presented refresh token is marked `RevokedAt` in the database and a new `RefreshTokens` row exists for the player

#### Scenario: Expired refresh token is rejected
- **WHEN** a client sends `POST /players/refresh` with a `cc_refresh` cookie whose stored token's `ExpiresAt` is in the past
- **THEN** the system responds `401 Unauthorized` and does NOT set a new cookie

#### Scenario: Absent refresh cookie is rejected
- **WHEN** a client sends `POST /players/refresh` with no `cc_refresh` cookie
- **THEN** the system responds `401 Unauthorized`

### Requirement: Replaying a revoked refresh token SHALL revoke all tokens for that player
If a refresh token that has already been revoked is presented, the system SHALL revoke all remaining active refresh tokens for the associated player and return `401 Unauthorized`, indicating a possible token theft event.

#### Scenario: Revoked token replay triggers full revocation
- **WHEN** a client presents a `cc_refresh` cookie whose token hash exists in the database with a non-null `RevokedAt`
- **THEN** all `RefreshTokens` rows for that player are set to `RevokedAt = now` and the system responds `401 Unauthorized`

### Requirement: Refresh tokens SHALL expire after 30 days
Every refresh token stored in the database SHALL have an `ExpiresAt` equal to the time of issuance plus 30 days (configurable via `Jwt__RefreshTokenExpiryDays`).

#### Scenario: Refresh token lifetime
- **WHEN** a refresh token is issued (either on registration or on a successful refresh)
- **THEN** the `ExpiresAt` column for that token equals `CreatedAt + 30 days` (tolerance ± 5 seconds)

### Requirement: Refresh token cookie SHALL use security-hardening attributes
The `cc_refresh` cookie set by `POST /players` and `POST /players/refresh` SHALL carry `HttpOnly`, `Secure` (configurable off in development), `SameSite=Strict`, `Path=/players/refresh`, and `Max-Age=2592000` (30 days in seconds).

#### Scenario: Cookie attributes on registration
- **WHEN** a successful `POST /players` response is received
- **THEN** the `Set-Cookie` header for `cc_refresh` includes `HttpOnly`, `SameSite=Strict`, `Path=/players/refresh`, and `Max-Age=2592000`

#### Scenario: Cookie security disabled in development
- **WHEN** `Jwt__CookieSecure` is set to `false` and a refresh cookie is issued
- **THEN** the `Set-Cookie` header does NOT include the `Secure` attribute
