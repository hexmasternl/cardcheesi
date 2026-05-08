## Why

Players have no way to register or start a game. This change delivers the first interactive flow: a player enters their name, is persisted to the database, receives a JWT identity token, and immediately creates a new game — giving every subsequent feature a player identity and game session to build on.

## What Changes

- Add a `POST /players` endpoint: accepts a player name, stores the player record in PostgreSQL, and returns a signed JWT containing the player's GUID.
- Add a `POST /games` endpoint (JWT-protected): creates a new game with the authenticated player as the first participant; returns a unique 6-character alphanumeric game code.
- Integrate PostgreSQL via the Aspire `postgres` hosting integration in the AppHost; use `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` in the API project.
- Wire the Aspire AppHost to launch the API and the PostgreSQL container together.
- Add EF Core migrations for the `Players` and `Games` tables.
- Add unit and integration tests covering registration, token issuance, and game creation.

## Capabilities

### New Capabilities

- `player-registration`: A player submits their name via `POST /players`; the server persists a `Player` record (GUID id, name, created-at) to PostgreSQL and returns a signed JWT token containing the player's GUID as the `sub` claim.
- `jwt-auth`: The server signs JWTs using a symmetric HMAC-SHA256 key configured via Aspire secrets / environment variables. Tokens have a configurable expiry (default 24 h). The API validates the token on protected endpoints and exposes the player GUID as the current identity.
- `game-creation`: An authenticated player calls `POST /games`; the server creates a `Game` record with a unique 6-character alphanumeric code (uppercase A–Z, 0–9) and associates the requesting player as the first participant. The game code is returned in the response.

### Modified Capabilities

<!-- No existing specs to modify. -->

## Impact

- **`src/card-cheesi.AppHost/AppHost.cs`**: add Aspire PostgreSQL resource and wire it to the API project.
- **`CardCheesi.Game.Api`**: new NuGet refs (`Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.AspNetCore.Authentication.JwtBearer`, `Microsoft.EntityFrameworkCore.Design`); new `AppDbContext`; EF Core migrations; new minimal-API endpoints.
- **`CardCheesi.Game.Abstractions`**: `Player` and `Game` domain records referenced by domain model change (depends on `create-game-domain-model`).
- **`CardCheesi.Game.Tests`**: new tests for registration and game creation; `Moq` + `Bogus` for test data; integration tests using `WebApplicationFactory`.
- **Infrastructure**: PostgreSQL container provisioned by Aspire in development; connection string injected via service discovery.
