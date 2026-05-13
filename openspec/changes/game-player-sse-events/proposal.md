## Why

Connected players have no real-time awareness of game membership changes or peer connection state. The SSE infrastructure exists but only broadcasts connection-level `player-status` events; there is no domain event for a player joining the game, and the frontend game state is never updated reactively from the stream.

## What Changes

- **Backend**: `JoinGameHandler` broadcasts a `player-joined` SSE event to all connected players when a new player successfully joins the game.
- **Backend**: The `player-status` event (Connected / Disconnected / Left) already exists and is broadcast by `PlayerPresenceTracker`; no changes needed to that path.
- **Frontend**: `SseService` is extended to handle the new `player-joined` event type alongside the existing `player-status` event.
- **Frontend**: `GamePage` reacts to `player-joined` SSE events by adding the incoming player to its local `gameState` signal, so the player list updates without a full HTTP re-fetch.
- **Frontend**: `PlayerPresencePanelComponent` / `PlayerPresenceStore` react to `player-joined` by seeding the new player into the presence map (defaulting to `Connected`).

## Capabilities

### New Capabilities

- `player-joined-sse`: Backend emits a `player-joined` SSE event to all connected game clients when a new player joins via the JoinGame command. Defines the event payload schema (`playerId`, `playerName`).
- `sse-frontend-event-handling`: Frontend `SseService` subscribes to both `player-joined` and `player-status` event types and exposes typed signals; `GamePage` integrates these signals to keep game state up to date reactively.

### Modified Capabilities

<!-- No existing spec-level requirements are changing. -->

## Impact

- `CardCheesi.Game/Features/JoinGame/JoinGameHandler.cs` — inject `ISseConnectionManager`, broadcast after save
- `CardCheesi.Game.Abstractions/SseEvent.cs` — no changes needed (generic record is sufficient)
- `src/App/src/app/services/sse.service.ts` — add `player-joined` event listener and signal
- `src/App/src/app/pages/game/game-page.ts` — consume `player-joined` signal via effect, update `gameState`
- `src/App/src/app/pages/game/player-presence-panel/player-presence.store.ts` — consume `player-joined` to seed new player as `Connected`
- No new npm packages or NuGet packages required
