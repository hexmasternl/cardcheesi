## 1. Aspire Infrastructure

- [ ] 1.1 Add `Aspire.Hosting.PostgreSQL` NuGet package to the AppHost project
- [ ] 1.2 Update `AppHost.cs`: declare a Postgres resource (`AddPostgres("postgres")`), add a `gamedb` database, and wire `.WithReference(gamedb)` on the API project reference
- [ ] 1.3 Verify the AppHost starts and the API connects to the Aspire-managed PostgreSQL container

## 2. JWT Configuration & Startup Validation

- [ ] 2.1 Add `Microsoft.AspNetCore.Authentication.JwtBearer` NuGet package to `CardCheesi.Game.Api`
- [ ] 2.2 Create a `JwtSettings` options class (`SigningKey`, `Issuer`, `Audience`, `ExpiryHours`) bound from the `Jwt` configuration section
- [ ] 2.3 Add startup validation: fail fast with a descriptive error if `Jwt__SigningKey` is absent or shorter than 32 bytes (use `ValidateOnStart` / `IStartupFilter`)
- [ ] 2.4 Register `AddAuthentication().AddJwtBearer(...)` in `Program.cs` using `JwtSettings`; add `app.UseAuthentication()` and `app.UseAuthorization()` to the pipeline
- [ ] 2.5 Add `Jwt__SigningKey`, `Jwt__Issuer`, and `Jwt__Audience` to the Aspire AppHost secrets / `appsettings.Development.json` (use a placeholder dev key of ≥ 32 bytes; never commit a production key)

## 3. Player Persistence

- [ ] 3.1 Add a `PlayerEntity` class to `CardCheesi.Game/Persistence/` with properties: `Id` (Guid, never generated), `Name` (varchar 50, required), `CreatedAt` (timestamptz)
- [ ] 3.2 Register `DbSet<PlayerEntity> Players` in `AppDbContext` and configure the entity: primary key on `Id`, unique index on `Name` is NOT required, `Name` max length 50
- [ ] 3.3 Add EF Core migration `AddPlayersTable` from `src/Game/CardCheesi.Game` with `--startup-project ../CardCheesi.Game.Api`
- [ ] 3.4 Verify `dotnet ef migrations list` shows the new migration and `update-database` applies cleanly against a local Postgres instance

## 4. Player Registration Endpoint

- [ ] 4.1 Create a `RegisterPlayerEndpoint` slice under `CardCheesi.Game.Api` (e.g., `Endpoints/Players/RegisterPlayerEndpoint.cs`) containing the handler, request record, and response record
- [ ] 4.2 Implement request validation: reject empty names, names > 50 chars, and names with leading/trailing whitespace or ASCII control characters — return `400 ValidationProblemDetails`
- [ ] 4.3 Implement handler: generate a new player GUID, persist `PlayerEntity`, build and sign a JWT (`sub` = GUID, `name` = display name, `iss`, `aud`, `iat`, `exp`), return `201 Created` with `{ "token": "<jwt>" }`
- [ ] 4.4 Map the endpoint in `Program.cs` as `POST /players` (no `[Authorize]`)
- [ ] 4.5 Ensure unhandled exceptions return a generic `500 ProblemDetails` with no stack trace or DB-specific message (configure `app.UseExceptionHandler` or equivalent)

## 5. Refactor Game Endpoints to Use JWT Identity

- [ ] 5.1 Remove the `PlayerName` field from `CreateGameRequest`; derive player GUID from `HttpContext.User` claim `sub` and player name from claim `name`
- [ ] 5.2 Remove the `PlayerName` field from `JoinGameRequest`; derive player identity from JWT claims in the same way
- [ ] 5.3 Add `.RequireAuthorization()` to both `POST /games` and `POST /games/{code}/join` endpoint registrations
- [ ] 5.4 Update `POST /games/{code}/join` response so `playerId` is the authenticated player's GUID (from JWT `sub`) rather than a freshly generated GUID

## 6. Unit & Integration Tests

- [ ] 6.1 Add unit tests for `RegisterPlayerEndpoint`: valid registration produces `201` + JWT, each invalid-name scenario produces `400` with correct field error, DB error produces `500` without leaking details
- [ ] 6.2 Add unit tests for JWT validation: missing key at startup throws, short key throws, valid token accepted, expired token rejected, wrong-key token rejected
- [ ] 6.3 Add integration tests (using existing `WebApplicationFactory` setup) for `POST /players`: round-trip registration and token verification
- [ ] 6.4 Update existing integration tests for `POST /games` and `POST /games/{code}/join` to supply a valid Bearer token; assert `401` when token is absent
- [ ] 6.5 Run `dotnet test src/card-cheesi.slnx --collect:"XPlat Code Coverage" /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total` and confirm the 80 % line-coverage threshold passes

## 7. OpenAPI Documentation

- [ ] 7.1 Add `.WithOpenApi()` and appropriate `Produces<>` / `ProducesProblem` annotations to `POST /players`
- [ ] 7.2 Add `SecurityRequirements` (Bearer scheme) annotation to `POST /games` and `POST /games/{code}/join` so Swagger UI shows the lock icon
- [ ] 7.3 Confirm the OpenAPI document is accessible at `/openapi/v1.json` in development and reflects all new/changed endpoints
