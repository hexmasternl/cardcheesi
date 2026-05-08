## 1. Project Setup

- [x] 1.1 Add `Npgsql.EntityFrameworkCore.PostgreSQL` NuGet reference to `CardCheesi.Game.Api` (if not already present from `create-new-game`)
- [x] 1.2 Add `Microsoft.EntityFrameworkCore.Design` NuGet reference to `CardCheesi.Game.Api`
- [x] 1.3 Add project reference from `CardCheesi.Game.Api` → `CardCheesi.Game` (if not already present)
- [x] 1.4 Add project reference from `CardCheesi.Game` → `CardCheesi.Game.Abstractions` (if not already present)

## 2. Domain Abstractions

- [x] 2.1 Define `IGameRepository` interface in `CardCheesi.Game.Abstractions` with `SaveAsync(GameState, CancellationToken)`, `GetByIdAsync(Guid, CancellationToken)`, and `GetByCodeAsync(string, CancellationToken)` methods
- [x] 2.2 Add `JsonConverter` for `PawnLocation` sealed class hierarchy in `CardCheesi.Game.Abstractions` to support `System.Text.Json` polymorphic serialization

## 3. EF Core Entity and DbContext

- [x] 3.1 Create `GameEntity` EF Core entity class in `CardCheesi.Game` with properties `Id` (Guid), `GameCode` (string), and `State` (GameState)
- [x] 3.2 Create `AppDbContext` in `CardCheesi.Game` (or `CardCheesi.Game.Api`) with a `DbSet<GameEntity> Games` property
- [x] 3.3 Configure `GameEntity` in `OnModelCreating`: set `Id` as UUID PK, `GameCode` as `varchar(6)` with unique index, `State` with `HasColumnType("jsonb")`

## 4. Repository Implementation

- [x] 4.1 Implement `GameRepository : IGameRepository` in `CardCheesi.Game` backed by `AppDbContext`
- [x] 4.2 Implement `SaveAsync` using EF Core upsert (`AddAsync` for new, `Update` for existing — check by `Id`)
- [x] 4.3 Implement `GetByIdAsync` querying `Games` table by `Id`
- [x] 4.4 Implement `GetByCodeAsync` querying `Games` table by `GameCode`

## 5. Database Migration

- [x] 5.1 Register `AppDbContext` and `IGameRepository` / `GameRepository` in `Program.cs` DI container
- [x] 5.2 Configure Aspire PostgreSQL connection string injection in `Program.cs` via `builder.AddNpgsqlDbContext<AppDbContext>("gamedb")`
- [x] 5.3 Wire PostgreSQL resource and connection in `AppHost.cs` (`AddPostgres` → `AddDatabase` → `WithReference` to API)
- [x] 5.4 Run `dotnet ef migrations add AddGamesTable --project CardCheesi.Game.Api` and verify generated migration contains `Id`, `GameCode`, `State` columns with unique index on `GameCode`

## 6. Update Game-Creation Endpoint

- [x] 6.1 Update `POST /games` handler to call `IGameRepository.SaveAsync` instead of any direct `DbContext` access
- [x] 6.2 Add retry loop for `GameCode` uniqueness: generate code, attempt save, catch unique-constraint exception, regenerate and retry (max 5 attempts)

## 7. Update Join-Game Endpoint

- [x] 7.1 Update `POST /games/{code}/join` handler to call `IGameRepository.GetByCodeAsync` for lookup
- [x] 7.2 Update join handler to call `IGameRepository.SaveAsync` with the updated `GameState` after adding the player
- [x] 7.3 Return HTTP 404 when `GetByCodeAsync` returns `null`

## 8. Tests

- [x] 8.1 Add unit tests for `GameRepository.SaveAsync` (new game inserted, existing game updated) using a mock `AppDbContext`
- [x] 8.2 Add unit tests for `GameRepository.GetByIdAsync` — found and not-found cases
- [x] 8.3 Add unit tests for `GameRepository.GetByCodeAsync` — found and not-found cases
- [x] 8.4 Add JSONB round-trip test: construct a `GameState` with pawns in all three location types, serialize and deserialize, assert structural equality
- [x] 8.5 Add integration test for `POST /games` verifying the game row appears in the database with correct `GameCode` and non-null `State`
- [x] 8.6 Add integration test for `POST /games/{code}/join` verifying the updated participant list is reflected in the `State` column

## 9. Verification

- [x] 9.1 Run `dotnet build src/card-cheesi.slnx` — no errors
- [x] 9.2 Run `dotnet test src/card-cheesi.slnx` — all tests green
- [ ] 9.3 Start the Aspire AppHost and verify the PostgreSQL container starts and the API connects successfully
