## Context

The CardCheesi backend is an ASP.NET Core Minimal API (.NET 10) backed by PostgreSQL via EF Core. The frontend is Angular 21 with Signals. Currently all game state is fetched via polling (`GET /games/{code}`) with a manual refresh button. Real-time updates were explicitly deferred in `game-view`.

Player presence is the first feature that requires server-push. The SSE infrastructure built here will be reused by future features (game state push, chat notifications, turn alerts). The design must be extensible — new event types should require zero changes to the transport layer.

## Goals / Non-Goals

**Goals:**
- Implement `GET /games/{code}/events` as a persistent SSE endpoint in ASP.NET Core
- Build a reusable `SseConnectionManager` that any future feature can use to broadcast named events
- Track per-player presence state (Connected / Disconnected / Left) with a configurable grace period
- Broadcast `player-status` events to all subscribers when presence changes
- Render a top-left overlay panel in the game page showing each player's live status

**Non-Goals:**
- WebSocket or SignalR transport — SSE is sufficient for server-to-client push; client-to-server communication continues via regular HTTP
- Persisting presence state to the database — connection state is transient
- Authentication / JWT validation on the SSE endpoint — deferred; `playerId` is passed as a query parameter matching the `localStorage` pattern already in use
- Horizontal scaling / multi-instance broadcasting — single-process in-memory is sufficient at this stage

## Decisions

### 1. SSE transport over WebSocket / SignalR
**Decision:** Use SSE (`text/event-stream`) as the real-time transport.
**Rationale:** SSE is a simple HTTP protocol natively supported by all target browsers. It is unidirectional (server → client) which is all that is needed. WebSocket and SignalR add bidirectional complexity and a runtime dependency that is not warranted.
**Alternative considered:** SignalR — deferred; adds a hub abstraction and client library overhead.

### 2. In-memory channel-per-connection via `Channel<T>`
**Decision:** Each SSE client connection is represented as a `Channel<SseEvent>`. The `SseConnectionManager` holds a `ConcurrentDictionary<string, List<SseChannel>>` keyed by game code.
**Rationale:** `System.Threading.Channels` provides a backpressure-aware, thread-safe async queue. The endpoint reads from its channel and writes SSE frames to the response body until the client disconnects or the `CancellationToken` fires.
**Alternative considered:** `IAsyncEnumerable` with a shared `BlockingCollection` — less composable and harder to fan out to N clients.

### 3. Presence state machine and grace-period timer
**Decision:** On SSE connection open, the player is marked `Connected`. On disconnect, a `CancellationTokenSource` with a 30-second delay is started; if the player reconnects before expiry the timer is cancelled and status returns to `Connected`. If the timer fires, status transitions to `Left` and a final `player-status` event is broadcast.
**Rationale:** Immediate `Left` on drop is too aggressive — page reloads and brief network blips would falsely evict players. 30 seconds is long enough to survive a reload but short enough to not leave stale presence data.
**Alternative considered:** Client heartbeat pings — more explicit but adds an extra endpoint and polling; the SSE keep-alive approach is simpler.

### 4. Event envelope schema
**Decision:** All events share a common envelope:
```
event: <event-type>
data: <JSON payload>
```
The `player-status` event payload:
```json
{ "playerId": "<guid>", "playerName": "<string>", "status": "Connected|Disconnected|Left" }
```
Future event types (e.g., `game-state`, `chat-message`) add new `event:` names without changing the transport.
**Rationale:** Named event types allow the frontend `EventSource` to `.addEventListener('player-status', handler)` without a discriminator field in the payload.

### 5. Frontend: `EventSource` wrapped in an Angular `SseService`
**Decision:** A singleton `SseService` manages one `EventSource` per active game code. It exposes typed `Signal<PlayerPresenceEvent[]>` that the `PlayerPresencePanelComponent` reads.
**Rationale:** Centralises connection lifecycle (open on game load, close on destroy via `DestroyRef`). Future components subscribe to the same service without opening duplicate connections.
**Alternative considered:** Direct `EventSource` in the component — causes duplicate connections if multiple components need the same stream.

### 6. Panel positioning and visual design
**Decision:** Fixed overlay at `top: 1rem; left: 1rem`. Semi-transparent dark background, PrimeNG `Tag` components for status badges. Status colours use SCSS variables from `_variables.scss`, not hardcoded hex values.
**Rationale:** Consistent with the CardCheesi theme. Using SCSS variables ensures dark-mode compatibility.

## Risks / Trade-offs

- **In-memory state lost on restart** — All players will show as `Left` after a server restart. → Mitigation: on reconnect the client re-opens the SSE stream, which re-registers the player as `Connected`. Acceptable for this stage.
- **No auth on SSE endpoint** — Any client knowing the game code and a player ID can subscribe. → Mitigation: game codes are 6-character random strings; formal auth is deferred to a future change.
- **Single-process only** — `SseConnectionManager` is in-process memory; horizontal scaling would require a message broker (Redis pub/sub). → Mitigation: encapsulate `SseConnectionManager` behind an interface (`ISseConnectionManager`) so it can be swapped for a distributed implementation later.
- **Browser SSE reconnect behaviour** — Browsers auto-reconnect `EventSource` after a network drop (default 3-second retry). This may cause a brief `Disconnected` flash during reconnect. → Mitigation: grace period is 30 seconds, so normal reconnect is transparent.
