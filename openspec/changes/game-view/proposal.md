## Why

Players need a dedicated in-game view after joining a game. Without it, there is no screen where the game board, hand, and game state are displayed — making the game unplayable from the frontend.

## What Changes

- Add a new Angular route `/game/:gameCode` that loads the `GamePage` component from `src/app/pages/game/`
- The page renders a full-screen background using a 3D render of the CardCheesi board
- The page displays player hands, pawn positions, and current game state pulled from the backend API
- A backend `GET /games/{code}` endpoint is exposed to retrieve game state by game code

## Capabilities

### New Capabilities

- `game-page`: The `/game/:gameCode` Angular route and its page component — full-screen 3D board background, game state display, player hand, and pawn positions

### Modified Capabilities

- `game-creation`: Add `GET /games/{code}` endpoint to the API so the frontend can retrieve game state by code

## Impact

- **Frontend**: new `src/app/pages/game/` component + route registered in `app.routes.ts`
- **Backend**: new `GET /games/{code}` endpoint in `CardCheesi.Game.Api/Program.cs` (reads via `IGameRepository.GetByCodeAsync`)
- **Assets**: a 3D board render image (exported from `3dmodels/board.blend`) placed in `src/assets/`
- **No breaking changes** to existing routes or API contracts
