## Context

The CardCheesi API already exposes `POST /games`, `GET /games/{code}`, and `POST /games/{code}/join` but has no concept of a persistent player identity. Player names are currently passed as plain strings in request bodies, with no authentication or persistence. The AppHost wires no external resources; PostgreSQL is implicitly expected via an externally-supplied connection string.

This design introduces:
- A **player registration** flow that persists a `Player` record and issues a short-lived JWT access token plus a long-lived refresh token.
- **JWT-based authentication** to secure mutating game endpoints.
- A **refresh token flow** — opaque token stored server-side (hashed) and delivered to the client as an HttpOnly cookie — that allows the frontend to silently renew access tokens without re-registration.
- **Player activity tracking** via a `LastSeenAt` timestamp updated on every token refresh.
- An **automatic cleanup** background service that removes players (and their tokens) who have been inactive for 31 days or longer.
- A **refactored `POST /games`** endpoint that derives player identity from the JWT rather than from a request body field.
- **Aspire-managed PostgreSQL** wired in the AppHost so the full stack starts with a single `dotnet run`.

Stakeholders: backend developer, frontend developer, infrastructure/DevOps.

## Goals / Non-Goals

**Goals:**
- Allow a player to register with a display name and receive a short-lived access token (10 min) and a long-lived refresh token (30 days).
- Persist a `Player` record (GUID, name, created-at, last-seen-at) to PostgreSQL.
- Store refresh tokens server-side as SHA-256 hashes; deliver the raw token to the client via an HttpOnly Secure cookie.
- Issue a new access token (and rotate the refresh token) via `POST /players/refresh` without requiring re-registration.
- Update `LastSeenAt` on every successful token refresh.
- Protect `POST /games` and `POST /games/{code}/join` with JWT authentication.
- Wire the Aspire AppHost to provision the PostgreSQL container and inject the connection string into the API.
- Add EF Core migrations for `Players` and `RefreshTokens` tables.
- Remove players and their tokens that have been inactive for ≥ 31 days via a daily background cleanup.
- Achieve ≥ 80 % unit-test line coverage for new code.

**Non-Goals:**
- Password-based login or OAuth / OpenID Connect.
- Player profile updates or account deletion.
- Multi-tenant or role-based authorization.
- Game state or board logic changes.
- Explicit logout / revoke-all-tokens endpoint (deferred to a later change).

## Decisions

### 1 — HMAC-SHA256 (HS256) symmetric JWT signing

**Chosen**: HS256 with a single symmetric key stored as an Aspire secret / environment variable (`Jwt__SigningKey`).

**Rationale**: There is one API service. Asymmetric signing (RS256/ES256) provides value when multiple independent services must validate tokens without sharing a secret. Adding key-pair management for a single service adds operational complexity without a security benefit at this stage.

**Alternative considered**: RS256 — ruled out because key rotation, key distribution, and JWKS endpoint work outweighs the benefit for a single-issuer, single-audience deployment.

**Risk**: If the signing key leaks an attacker can forge tokens. **Mitigation**: key is never committed to source control; it is injected via Aspire secrets (development) and environment-specific secrets management (production); minimum key length enforced at startup (≥ 32 bytes / 256 bits).

---

### 2 — Dedicated `Players` table; no JWT payload-only identity

**Chosen**: Persist `PlayerEntity` (`Id` GUID, `Name` varchar(50), `CreatedAt` timestamptz) in a new `Players` table. The JWT `sub` claim carries the player GUID; downstream handlers resolve the GUID to a name via the DB when needed.

**Rationale**: Storing identity only in the JWT makes player-lookup queries impossible and prevents server-side revocation in future. A lightweight `Players` table is low cost and enables future features (friends list, statistics, ban).

**Alternative considered**: Embed all player data in the JWT, no DB row. Ruled out because it forecloses future features and makes the player name the only identifier, which is mutable in principle.

---

### 3 — Refactor existing `POST /games` and `POST /games/{code}/join` to use JWT identity

**Chosen**: Remove the `PlayerName` field from `CreateGameRequest` and `JoinGameRequest`. The player GUID and name are read from the validated JWT claims (`sub` and `name`). The `Player` value object is constructed from these claims without an extra DB round-trip per request.

**Rationale**: Having both a bearer token and a body-supplied player name creates a redundancy that could be abused (spoofing another player's name). Deriving identity exclusively from the token removes that surface.

**Alternative considered**: Keep `PlayerName` in the request body and treat it as display-name override per game. Ruled out because it breaks the single-source-of-truth principle for player identity.

---

### 4 — Aspire PostgreSQL resource added to AppHost

**Chosen**: Add `builder.AddPostgres("postgres").AddDatabase("gamedb")` to `AppHost.cs` and reference it from the API project via `.WithReference(gamedb)`.

**Rationale**: The current AppHost is empty; EF Core migrations work but require an externally managed connection string. Making the AppHost authoritative brings the project in line with guideline 0003 and ensures `dotnet run --project src/card-cheesi.AppHost` is the single command needed for local development.

**Alternative considered**: External Docker Compose. Ruled out because Aspire provides richer dev-time telemetry and service discovery that the rest of the stack already expects.

---

### 5 — Input validation via minimal-API endpoint filters

**Chosen**: Use `IEndpointFilter` (or inline `Results.ValidationProblem`) to validate player name: non-empty, 1–50 characters, no control characters. Return `400 Bad Request` with a structured `ValidationProblemDetails` body.

**Rationale**: Keeps validation co-located with each endpoint slice (Vertical Slice Architecture — guideline 0007) without pulling in a full FluentValidation dependency.

**Alternative considered**: FluentValidation with a registration-wide filter. Ruled out as over-engineering for two endpoints.

---

### 6 — Refresh tokens stored as SHA-256 hashes; delivered via HttpOnly cookie

**Chosen**: On registration (and on every refresh), generate a 256-bit cryptographically random value (`RandomNumberGenerator.GetBytes(32)`). Store only `SHA256(token)` in the `RefreshTokens` table. Return the raw token to the client as a cookie with the following attributes:
- `HttpOnly` — not accessible from JavaScript
- `Secure` — transmitted over HTTPS only (relaxed to allow HTTP in development via configuration)
- `SameSite=Strict` — not sent on cross-site requests; prevents CSRF on the refresh endpoint
- `Path=/players/refresh` — cookie is only sent to the refresh endpoint
- `Max-Age` = 30 days (2592000 seconds)
- Cookie name: `cc_refresh`

**Rationale**: Storing the raw token in the database would make a DB dump directly exploitable. Hashing with SHA-256 means a DB breach yields only hashes, not usable tokens. The HttpOnly + SameSite=Strict combination removes the two primary browser-side attack vectors (XSS and CSRF).

**Alternative considered**: Storing raw tokens in DB (simpler code). Ruled out on security grounds.

**Alternative considered**: `SameSite=Lax`. Ruled out — `Strict` is preferable because the refresh endpoint is purely an API endpoint not reachable via top-level navigation; `Strict` provides stronger CSRF protection with no UX penalty.

---

### 7 — Refresh token rotation with theft-detection

**Chosen**: On every call to `POST /players/refresh`, the presented refresh token is immediately revoked (`RevokedAt` is set) and a new token is issued. If a token that is already revoked is presented, all active refresh tokens for that player are revoked and a `401` is returned — indicating possible token theft.

**Rationale**: Rotation ensures that a stolen refresh token can only be used once before the legitimate user's next refresh invalidates it and triggers the theft-detection path. The full-revocation response to a replayed token limits the blast radius of a successful theft.

**Alternative considered**: No rotation (single long-lived token). Ruled out — a stolen token would be valid for up to 30 days with no server-side recourse.

---

### 8 — Access tokens expire in 10 minutes; refresh tokens expire in 30 days

**Chosen**: `JwtSettings.AccessTokenExpiryMinutes = 10` (configurable), `JwtSettings.RefreshTokenExpiryDays = 30` (configurable). The frontend silently calls `POST /players/refresh` before the access token expires to obtain a new one.

**Rationale**: Short-lived access tokens limit the window of exposure if a token is leaked from memory or logs. The refresh token's 30-day lifetime matches the inactivity cleanup window so a returning user can always transparently restore their session without re-registering.

**Alternative considered**: 24-hour access tokens. Ruled out — a 24-hour window is too long for a bearer token carried in memory and HTTP headers.

---

### 9 — Player activity tracking and cleanup via BackgroundService

**Chosen**: Add a `LastSeenAt` (timestamptz, nullable initially then backfilled) column to `Players`. Set it on registration and update it on every successful `POST /players/refresh`. A `PlayerCleanupService : BackgroundService` runs a daily sweep: it deletes `RefreshTokens` whose `ExpiresAt < now` and deletes `Players` (cascading to their tokens) whose `LastSeenAt < now - 31 days`.

**Rationale**: The cleanup window (31 days) is one day longer than the refresh token lifetime (30 days), ensuring that a player who just refreshed their token is never removed in the same cycle. Deleting inactive players keeps the database lean and avoids accumulating anonymous records.

**Alternative considered**: Cron job / external scheduler. Ruled out — a hosted `BackgroundService` keeps the cleanup logic in the same deployable unit with no additional infrastructure.

**Risk**: A long-running game session (> 10 min) with no explicit token refresh would expire the access token mid-game. **Mitigation**: The Angular client implements a proactive refresh 60 seconds before the access token's `exp`. The game socket/API layer returns `401` which the frontend intercepts to trigger a refresh.

## Risks / Trade-offs

| Risk | Mitigation |
|------|------------|
| Signing key not set in environment → startup crash | Validate key presence and minimum length in `IStartupFilter` or `ValidateOnStart`; log actionable error message |
| Short player name uniqueness not enforced → duplicate names allowed | Names are display-only; uniqueness is tracked via GUID `sub` claim; no deduplication needed |
| Game code collision under high load | Existing retry loop (5 attempts) is retained; at > 10 M active codes the probability increases — addressed in a future scaling change |
| Access token (10 min) expires mid-game | Frontend proactively refreshes 60 s before expiry; `401` intercept triggers refresh before retrying the failed call |
| Refresh token stolen from cookie | SHA-256 storage + HttpOnly + Secure + SameSite=Strict + Path scoping minimises attack surface; theft-detection rotation revokes all tokens on replay |
| Cleanup accidentally removes an active player | Cleanup window (31 days) > token lifetime (30 days); a player who refreshed today will have `LastSeenAt = today`, well outside the deletion window |
| `POST /games/{code}/join` now returns the caller's `playerId` from JWT | No longer generates a new GUID per join — callers that relied on the returned `playerId` for routing will get the same GUID they already have, which is safer |
| Cookie `Secure` flag blocks HTTP in development | Disable `Secure` flag in development via `Jwt__CookieSecure = false`; always `true` in production |

## Migration Plan

1. Add `AddPlayersTable` EF Core migration (includes `LastSeenAt`).
2. Add `AddRefreshTokensTable` EF Core migration.
3. Deploy updated API (new `POST /players`, new `POST /players/refresh`, auth middleware, cleanup background service).
4. Existing `POST /games` and `POST /games/{code}/join` callers must supply a Bearer token; unauthenticated calls return `401`.
5. The Angular frontend is updated (in the same change) to call `POST /players/refresh` on app initialisation if the `cc_refresh` cookie exists, store the returned access token in memory, and proactively refresh 60 s before expiry.

**Rollback**: Revert to previous API image. `Players` and `RefreshTokens` tables can be left in place (additive migrations). The cleanup service is a no-op if no rows match the criteria.

## Open Questions

- Should player names be unique per game session or globally unique? **Current decision**: names are display-only, not unique. Revisit if leaderboard features are added.
- Should the JWT issuer/audience be configurable per environment? **Current decision**: configurable via `Jwt__Issuer` and `Jwt__Audience` settings with sensible defaults (`cardcheesi-api`).
- Should a player be able to hold multiple concurrent refresh tokens (e.g., two browser tabs)? **Current decision**: yes — multiple active `RefreshTokens` rows per player are allowed; rotation invalidates only the token that was presented, not all others. Theft detection fires only when a revoked token is presented.
