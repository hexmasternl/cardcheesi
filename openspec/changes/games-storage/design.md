## Context

Prior changes (`create-new-game`, `join-game`) established the `Games` table conceptually but left its column structure undefined. Game state (`GameState`) is a deeply nested domain object containing players, pawns, cards, board positions, and turn state. Mapping this to relational columns would require 8–10 tables and a migration for every domain model evolution.

PostgreSQL's native `JSONB` type offers a pragmatic middle ground: the table stays structurally stable (three columns), the full domain object round-trips through a single column, and PostgreSQL can still index into the JSON document when query patterns demand it.

## Goals / Non-Goals

**Goals:**
- Define the `Games` table schema: `Id` (UUID PK), `GameCode` (varchar 6, unique), `State` (JSONB).
- Map the EF Core entity to this schema using Npgsql's JSONB column support.
- Introduce `IGameRepository` as the single abstraction for game persistence.
- Update game-creation and join-game flows to use `IGameRepository`.
- Add EF Core migration for the new schema.
- Verify JSONB round-trip integrity in tests.

**Non-Goals:**
- Full-text or deep JSONB querying beyond lookup by `Id` / `GameCode`.
- Event sourcing or audit log of state changes.
- Optimistic concurrency / row-version conflict handling (future concern once real-time play is added).
- Separate `Players` table (covered by `create-new-game`).

## Decisions

### 1. JSONB column for `GameState` instead of full relational mapping

**Decision**: Store `GameState` as a single `jsonb` column named `State`.

**Rationale**: The domain model (`create-game-domain-model`) defines `GameState` as an immutable record graph. Relational mapping would require 8+ tables, complex joins on every read, and a migration any time the domain model adds a field. JSONB keeps the database schema stable, reads in a single query, and writes atomically. PostgreSQL JSONB is indexed and queryable if specific fields are needed later.

**Alternatives considered**:
- Full ORM mapping — too much schema churn as the domain model matures.
- `text` column with JSON string — loses PostgreSQL's ability to validate, index, and query the JSON.
- Separate event log — overengineered for the current stage.

---

### 2. Three-column `Games` table

**Decision**: `Id UUID PRIMARY KEY`, `GameCode VARCHAR(6) UNIQUE NOT NULL`, `State JSONB NOT NULL`.

**Rationale**: `Id` is the internal surrogate key (used in foreign keys, URLs). `GameCode` is the human-facing lobby code — promoted to a first-class indexed column so `GET /games/{code}` resolves in O(log n) without scanning JSONB. Everything else is inside `State`.

---

### 3. `IGameRepository` abstraction in `CardCheesi.Game.Abstractions`

**Decision**: Define `IGameRepository` with `SaveAsync(GameState)`, `GetByIdAsync(Guid)`, and `GetByCodeAsync(string)`. Implement in `CardCheesi.Game` as `GameRepository : IGameRepository`.

**Rationale**: Decouples the API and domain logic from EF Core. Tests can inject a mock repository without needing a database. Follows the project's existing pattern of abstractions in the `Abstractions` project.

---

### 4. Npgsql EF Core JSON column mapping (`HasColumnType("jsonb")`)

**Decision**: Use `modelBuilder.Entity<GameEntity>().Property(e => e.State).HasColumnType("jsonb")` with `System.Text.Json` serialization (Npgsql default).

**Rationale**: Npgsql handles serialization transparently; no custom converter boilerplate. `System.Text.Json` is already part of the .NET 10 runtime — no extra dependency.

**Consequence**: `GameState` and all nested records must be serializable by `System.Text.Json` (use `[JsonConstructor]` or primary-constructor records as needed).

---

### 5. Unique index on `GameCode` at the database level

**Decision**: Add a unique constraint/index on `GameCode` in the EF migration, in addition to application-level uniqueness checks.

**Rationale**: Prevents duplicate codes under concurrent game creation (race condition). The application checks first for a friendly error; the database is the safety net.

## Risks / Trade-offs

- [JSONB column grows unbounded as game progresses] → `GameState` for a 4-player game is small (< 10 KB); no concern in practice.
- [System.Text.Json may struggle with sealed class hierarchies (`PawnLocation`)] → Mitigation: configure a `JsonConverter` for the `PawnLocation` discriminated union; add a round-trip test to catch regressions early.
- [No optimistic concurrency] → Two simultaneous writes (e.g., two players moving at once) could cause a lost update. → Mitigation: acceptable now; add EF Core `RowVersion` / PostgreSQL `xmin` in the real-time play change.

## Migration Plan

1. Add `Npgsql.EntityFrameworkCore.PostgreSQL` (already planned in `create-new-game`) to the API project.
2. Create `GameEntity` (EF persisted entity) and `GameRepository`.
3. Configure JSONB mapping in `AppDbContext.OnModelCreating`.
4. Run `dotnet ef migrations add AddGamesTable` to generate the migration.
5. Update game-creation and join endpoints to call `IGameRepository`.
6. Run `dotnet test` — JSONB round-trip tests must pass.

## Open Questions

- Should `GameCode` be stored in the `State` JSONB as well, or only as a first-class column? *(Suggest: column only — avoids duplication and keeps `GameState` clean.)*
- Should `Id` also appear inside `State`? *(Suggest: yes — `GameState.Id` is part of the domain model; the EF entity column is the persisted projection of it.)*
