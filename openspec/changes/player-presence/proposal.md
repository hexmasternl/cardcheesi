## Why

Players in a game have no visibility into who is connected and actively present. Without real-time presence indicators, a player cannot tell if opponents are still watching, have lost their connection, or have abandoned the game — making it impossible to know whether to wait or act.

## What Changes

- Add a `GET /games/{code}/events` SSE endpoint that pushes typed events to all connected clients; this is the shared real-time infrastructure for all future server-push features
- Track per-player connection state in-memory on the server: `Connected`, `Disconnected` (grace period), and `Left` (grace period expired or explicit leave)
- Broadcast `player-status` events over the SSE stream whenever a player's presence state changes
- Add a top-left overlay panel on the game page (`PlayerPresencePanel`) that subscribes to the SSE stream and displays each player's name and live connection status with a colour-coded indicator

## Capabilities

### New Capabilities

- `sse-infrastructure`: The `GET /games/{code}/events` SSE endpoint — a persistent `text/event-stream` HTTP response that fans out typed events (`player-status`, and future types) to all clients subscribed to a game
- `player-presence`: Per-player connection tracking (Connected / Disconnected / Left) maintained in-memory on the server; state changes are broadcast as `player-status` SSE events; frontend `PlayerPresencePanel` overlay consumes the stream and renders live status indicators

### Modified Capabilities

## Impact

- **`CardCheesi.Game.Api`**: new `GET /games/{code}/events` SSE endpoint; new `SseConnectionManager` service (in-memory connection registry + broadcaster); player presence state machine and grace-period timer logic; DI registration
- **`CardCheesi.Game.Abstractions`**: new `PlayerPresenceStatus` enum (`Connected`, `Disconnected`, `Left`) and `PlayerPresenceEvent` record used as the SSE payload
- **`CardCheesi.Game.Tests`**: unit tests for `SseConnectionManager` (connect, disconnect, grace period expiry, broadcast); integration test for the SSE endpoint
- **Frontend (`src/App/CardCheesi`)**: new `PlayerPresencePanelComponent` standalone component anchored top-left; new `SseService` wrapping the browser `EventSource` API; `GamePage` wired to subscribe on load and unsubscribe on destroy
- **No database changes** — presence state is transient and lives in-memory only
