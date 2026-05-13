## Context

CardCheesi already has an SSE infrastructure: `ISseConnectionManager` (broadcast to all connections in a game), `IPlayerPresenceTracker` (tracks online/offline/left status), and `SseGameEventService` (manages the per-connection channel and presence lifecycle). The frontend `SseService` opens an `EventSource` and handles `player-status` events; `PlayerPresenceStore` maintains a signal-based map of peer statuses.

What is missing is a **domain-level game event** for the "player joined the game" action. Right now `JoinGameHandler` persists the new player but does not notify connected clients. The frontend game state (`GamePage.gameState`) is also immutable post-load — it does not react to incoming SSE events.

## Goals / Non-Goals

**Goals:**
- Broadcast a `player-joined` SSE event to all connected clients when a player joins via `JoinGameHandler`
- Frontend `SseService` handles `player-joined` and `player-status` with typed signals
- `GamePage` reacts to `player-joined` signals and appends the new player to its game state
- `PlayerPresenceStore` seeds a newly joined player as `Connected` upon receiving `player-joined`

**Non-Goals:**
- Securing the SSE endpoint with JWT (EventSource does not support custom headers; query-param playerId is an accepted trade-off for this iteration)
- Handling game state updates for events beyond joining (e.g., card played, game started) — those belong to future changes
- Persistent event replay / catching up clients who missed events (no event sourcing in this iteration)

## Decisions

### Decision 1: Inject `ISseConnectionManager` directly into `JoinGameHandler`

**Chosen**: Inject `ISseConnectionManager` into `JoinGameHandler` and call `BroadcastAsync` after a successful save.

**Alternative considered**: Introduce a domain event bus / mediator pipeline. This would be cleaner long-term but adds significant complexity for a single event. The connection manager is already a singleton; direct injection keeps the handler simple and consistent with how other handlers access dependencies.

**Rationale**: Keeps complexity low. The handler already throws domain exceptions; broadcasting SSE after save is a natural extension of the command result.

### Decision 2: Separate `player-joined` event type from `player-status`

**Chosen**: Emit `player-joined` (with payload `{ playerId, playerName }`) as a distinct event type, separate from the existing `player-status` event (which carries `{ playerId, playerName, status }`).

**Alternative considered**: Reuse `player-status` with a new `Joined` status. Rejected because "joined" is a domain event (added to the game roster) while `Connected`/`Disconnected`/`Left` are connection-lifecycle events — conflating them makes the frontend harder to evolve.

**Rationale**: Clear separation of domain events vs. connection events; frontend can handle each independently.

### Decision 3: Update `GamePage` game state via effect, not HTTP re-fetch

**Chosen**: Use an Angular `effect(() => { ... })` in `GamePage` that watches the `playerJoined` signal on `SseService` and appends the new player to the `gameState` signal.

**Alternative considered**: Re-fetch the full game via HTTP on every `player-joined` event. Simple but unnecessary round-trip and may cause race conditions under rapid joins.

**Rationale**: The SSE payload carries all required data (`playerId`, `playerName`). Appending locally is instant and avoids extra HTTP traffic.

## Risks / Trade-offs

- **Risk**: `JoinGameHandler` is scoped (`AddScoped`) but `ISseConnectionManager` is a singleton — safe to inject directly (singleton-in-scoped is fine in .NET DI).
- **Risk**: `BroadcastAsync` can fail if a channel is full or closed during broadcast — the existing `SseConnectionManager` swallows closed-channel writes gracefully; no changes needed.
- **Risk**: Frontend `gameState` signal may diverge from server state if a `player-joined` event is missed (e.g., network hiccup before SSE reconnect) → Mitigation: `EventSource` auto-reconnects; on reconnect the `StreamEventsAsync` replays the presence snapshot, and `GamePage` could do an optional HTTP re-fetch on reconnect (out of scope for this change).
- **Trade-off**: Duplicated player in `gameState` if `player-joined` fires while an HTTP re-fetch is in flight → Mitigation: deduplicate by `id` when applying the event.
