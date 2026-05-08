## Why

Once a game is created it needs up to 3 more players before it can start. Without a join flow, the game can never reach 4 players. This change adds the `POST /games/{code}/join` endpoint so registered players can enter a waiting game using its 6-character code.

## What Changes

- Add a `POST /games/{code}/join` endpoint (JWT-protected): looks up the game by its 6-character code, validates the game is joinable, adds the authenticated player as a participant, and returns the updated game state.
- Enforce join constraints: maximum 4 players, game must be in `Waiting` status, player must not already be in the game.
- Store the `GamePlayer` association (game id + player id + join order) in PostgreSQL via EF Core (new join table or column on the existing `Games` table).
- Add unit and integration tests covering the join flow and all validation error cases.

## Capabilities

### New Capabilities

- `join-game`: A JWT-authenticated player submits `POST /games/{code}/join`; the server validates the code resolves to a waiting game with fewer than 4 participants and that the player is not already a member, then adds the player and returns the current participant list and game status.

### Modified Capabilities

- `game-creation`: The `Game` entity must track a `Status` field (`Waiting` | `InProgress` | `Finished`) and an ordered list of participant player IDs so that the join endpoint can evaluate capacity and membership. The initial status when a game is created is `Waiting`.

## Impact

- **`CardCheesi.Game.Api`**: new `POST /games/{code}/join` minimal-API endpoint; `GamePlayer` EF entity (join table); new EF Core migration.
- **`CardCheesi.Game.Abstractions`**: `GameStatus` enum (`Waiting`, `InProgress`, `Finished`) added to the domain model; `Game` record gains `Status` and `Players` collection.
- **`CardCheesi.Game.Tests`**: new unit + integration tests for join flow; error cases (game not found, game full, already joined).
- **No Aspire or frontend changes** required for this change.
