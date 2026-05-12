## 1. Aspire Infrastructure

- [ ] 1.1 Add `Aspire.Hosting.PostgreSQL` NuGet package to the AppHost project
- [ ] 1.2 Update `AppHost.cs`: declare a Postgres resource (`AddPostgres("postgres")`), add a `gamedb` database, and wire `.WithReference(gamedb)` on the API project reference
- [ ] 1.3 Verify the AppHost starts and the API connects to the Aspire-managed PostgreSQL container

## 2. JWT Configuration & Startup Validation

- [ ] 2.1 Add `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package to `CardCheesi.Game.Api`
- [ ] 2.2 Create a `JwtSettings` options class with properties: `SigningKey`, `Issuer`, `Audience`, `AccessTokenExpiryMinutes` (default 10), `RefreshTokenExpiryDays` (default 30), `CookieSecure` (default true); bind from the `Jwt` configuration section
- [ ] 2.3 Add startup validation: fail fast with a descriptive error if `Jwt__SigningKey` is absent or shorter than 32 bytes (use `ValidateOnStart` / `IStartupFilter`)
- [ ] 2.4 Register `AddAuthentication().AddJwtBearer(...)` in `Program.cs` using `JwtSettings`; add `app.UseAuthentication()` and `app.UseAuthorization()` to the pipeline
- [ ] 2.5 Add `Jwt__SigningKey`, `Jwt__Issuer`, `Jwt__Audience`, and `Jwt__CookieSecure=false` to the Aspire AppHost secrets / `appsettings.Development.json` (use a placeholder dev key of ≥ 32 bytes; never commit a production key)

## 3. Player & Refresh Token Persistence

- [ ] 3.1 Add a `PlayerEntity` class to `CardCheesi.Game/Persistence/` with properties: `Id` (Guid, never generated), `Name` (varchar 50, required), `CreatedAt` (timestamptz), `LastSeenAt` (timestamptz, required)
- [ ] 3.2 Add a `RefreshTokenEntity` class with properties: `Id` (Guid, never generated), `PlayerId` (Guid, FK → Players.Id, cascade delete), `TokenHash` (varchar 64, required — hex-encoded SHA-256), `CreatedAt` (timestamptz), `ExpiresAt` (timestamptz), `RevokedAt` (timestamptz, nullable)
- [ ] 3.3 Register `DbSet<PlayerEntity> Players` and `DbSet<RefreshTokenEntity> RefreshTokens` in `AppDbContext`; configure: PK on `Id` for both, unique index on `RefreshTokens.TokenHash`, FK with cascade delete from `RefreshTokens` to `Players`, `Name` max length 50
- [ ] 3.4 Add EF Core migration `AddPlayersTable` from `src/Game/CardCheesi.Game` with `--startup-project ../CardCheesi.Game.Api`
- [ ] 3.5 Add EF Core migration `AddRefreshTokensTable` in the same way
- [ ] 3.6 Verify `dotnet ef migrations list` shows both new migrations and `update-database` applies cleanly

## 4. Player Registration Endpoint

- [ ] 4.1 Create a `RegisterPlayerEndpoint` slice under `CardCheesi.Game.Api` (e.g., `Endpoints/Players/RegisterPlayerEndpoint.cs`) containing the handler, request record, and response record
- [ ] 4.2 Implement request validation: reject empty names, names > 50 chars, and names with leading/trailing whitespace or ASCII control characters — return `400 ValidationProblemDetails`
- [ ] 4.3 Implement handler: generate a new player GUID; set `CreatedAt` and `LastSeenAt` to now; persist `PlayerEntity`; generate a cryptographically random 32-byte refresh token, hash it with SHA-256, persist `RefreshTokenEntity` (`ExpiresAt = now + 30 days`); build and sign the access JWT (`sub`, `name`, `iss`, `aud`, `iat`, `exp = now + 10 min`); return `201 Created` with `{ "token": "<jwt>" }` and a `Set-Cookie: cc_refresh` header
- [ ] 4.4 Set `cc_refresh` cookie attributes: `HttpOnly`, `Secure` (from `JwtSettings.CookieSecure`), `SameSite=Strict`, `Path=/players/refresh`, `Max-Age=2592000`
- [ ] 4.5 Map the endpoint in `Program.cs` as `POST /players` (no `[Authorize]`)
- [ ] 4.6 Ensure unhandled exceptions return a generic `500 ProblemDetails` with no stack trace or DB-specific message (configure `app.UseExceptionHandler` or equivalent)

## 5. Token Refresh Endpoint

- [ ] 5.1 Create a `RefreshTokenEndpoint` slice (`Endpoints/Players/RefreshTokenEndpoint.cs`)
- [ ] 5.2 Implement handler: read the `cc_refresh` cookie value; return `401` if absent; compute SHA-256 hash; look up `RefreshTokenEntity` by hash
- [ ] 5.3 If token not found → return `401`
- [ ] 5.4 If token found but `RevokedAt` is set → revoke ALL active `RefreshTokens` for that player (theft-detection); return `401`
- [ ] 5.5 If token found but `ExpiresAt < now` → return `401` (do not set a new cookie)
- [ ] 5.6 On valid token: set `RevokedAt = now` on the old token; generate a new refresh token + hash, persist new `RefreshTokenEntity`; update `player.LastSeenAt = now`; issue a new access JWT; return `200 OK` with `{ "token": "<new-jwt>" }` and a new `cc_refresh` `Set-Cookie` header with the same attributes as registration
- [ ] 5.7 Map the endpoint in `Program.cs` as `POST /players/refresh` (no `[Authorize]`)

## 6. Refactor Game Endpoints to Use JWT Identity

- [ ] 6.1 Remove the `PlayerName` field from `CreateGameRequest`; derive player GUID from `HttpContext.User` claim `sub` and player name from claim `name`
- [ ] 6.2 Remove the `PlayerName` field from `JoinGameRequest`; derive player identity from JWT claims in the same way
- [ ] 6.3 Add `.RequireAuthorization()` to both `POST /games` and `POST /games/{code}/join` endpoint registrations
- [ ] 6.4 Update `POST /games/{code}/join` response so `playerId` is the authenticated player's GUID (from JWT `sub`) rather than a freshly generated GUID

## 7. Player Cleanup Background Service

- [ ] 7.1 Create `PlayerCleanupService : BackgroundService` in `CardCheesi.Game.Api` (or `CardCheesi.Game`)
- [ ] 7.2 Implement the daily sweep: delete `RefreshTokens` where `ExpiresAt < now`; delete `Players` (cascade) where `LastSeenAt < now - 31 days`
- [ ] 7.3 Log the count of deleted players and tokens after each sweep at `Information` level
- [ ] 7.4 Register `PlayerCleanupService` with `builder.Services.AddHostedService<PlayerCleanupService>()` in `Program.cs`
- [ ] 7.5 Add a configurable sweep interval (default 24 h) via `Cleanup__IntervalHours` setting to support faster cycling in tests

## 8. Angular — Token Lifecycle & Auto-Restore

- [ ] 8.1 Create an `AuthService` in the Angular app that stores the access token in memory (a `signal<string | null>`) and exposes `isAuthenticated: Signal<boolean>`
- [ ] 8.2 Implement `AuthService.tryRestoreSession()`: call `POST /players/refresh`; on success store the token; on `401` clear any stored state
- [ ] 8.3 Call `tryRestoreSession()` in an `APP_INITIALIZER` so it runs before the router activates any route
- [ ] 8.4 Implement proactive refresh: schedule a call to `POST /players/refresh` 60 seconds before the current access token's `exp` (parse expiry from the JWT payload without a library)
- [ ] 8.5 Add an HTTP interceptor that attaches `Authorization: Bearer <token>` to all API requests and retries once after a `401` by calling `POST /players/refresh` first
- [ ] 8.6 Route guard: redirect unauthenticated users to the registration page; redirect authenticated users away from the registration page

## 9. Unit & Integration Tests

- [ ] 9.1 Add unit tests for `RegisterPlayerEndpoint`: valid registration produces `201` + JWT + `cc_refresh` cookie with correct attributes, each invalid-name scenario produces `400`, DB error produces `500` without leaking details
- [ ] 9.2 Add unit tests for `RefreshTokenEndpoint`: valid token → `200` + new JWT + new cookie + old token revoked; absent cookie → `401`; expired token → `401`; revoked token → all-revoke + `401`
- [ ] 9.3 Add unit tests for JWT validation: missing key at startup throws, short key throws, valid token accepted, expired token rejected, wrong-key token rejected
- [ ] 9.4 Add unit tests for `PlayerCleanupService`: inactive player deleted, active player retained, expired token cleaned up independently, no-op run succeeds
- [ ] 9.5 Add integration tests (using `WebApplicationFactory`) for the full registration → refresh → game-create flow
- [ ] 9.6 Update existing integration tests for `POST /games` and `POST /games/{code}/join` to supply a valid Bearer token; assert `401` when token is absent
- [ ] 9.7 Run `dotnet test src/card-cheesi.slnx --collect:"XPlat Code Coverage" /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total` and confirm the 80 % line-coverage threshold passes

## 10. OpenAPI Documentation

- [ ] 10.1 Add `.WithOpenApi()` and appropriate `Produces<>` / `ProducesProblem` annotations to `POST /players` and `POST /players/refresh`
- [ ] 10.2 Add `SecurityRequirements` (Bearer scheme) annotation to `POST /games` and `POST /games/{code}/join` so Swagger UI shows the lock icon
- [ ] 10.3 Confirm the OpenAPI document is accessible at `/openapi/v1.json` in development and reflects all new/changed endpoints
