## ADDED Requirements

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
