## Context

CardCheesi already has an SSE streaming infrastructure (`SseGameEventService`) that pushes real-time events (presence, your-turn) to players connected to a game. The game page has a HUD panel that slides in from the bottom. The frontend uses Angular Signals and the `SseService` to consume these events. Chat is a natural extension of this infrastructure — messages are posted via HTTP and fanned out to all connected players via SSE.

Chat messages are ephemeral: they exist only for the duration of the game session and are not persisted to the database. This keeps complexity low while delivering the real-time social experience.

## Goals / Non-Goals

**Goals:**
- Allow any player in an active game to send a text message.
- Broadcast that message to all connected players in real time via SSE.
- Display messages in a slide-in chat panel on the left side of the game page (mirroring the HUD on the bottom).
- Show an unread badge on the toggle button when the panel is collapsed and new messages arrive.

**Non-Goals:**
- Message persistence across server restarts or new sessions.
- Private / direct messages between players.
- Chat moderation, filtering, or admin tools.
- Message history for players who join after messages were sent.
- Rich text, emoji picker, or file attachments.

## Decisions

### 1. In-memory message fan-out via existing SSE stream

**Decision**: Reuse `SseGameEventService` to broadcast `chat-message` events. The `POST /games/{code}/chat` handler publishes the message to an in-memory channel keyed by game code; the SSE service picks it up and emits it to all open SSE connections for that game.

**Alternatives considered**:
- **SignalR hub**: More capable but adds a new dependency and protocol. Overkill given SSE already works.
- **Polling endpoint**: Simple but defeats the real-time goal and adds unnecessary load.

**Why this**: Zero new infrastructure, consistent with presence and turn events, ships fast.

### 2. No persistence — ephemeral messages only

**Decision**: Chat messages are stored only in memory (a bounded `ConcurrentQueue<ChatMessageDto>` on a scoped game session object or as a simple fan-out without storage).

**Alternatives considered**:
- **Persist to DB (EF Core)**: Would require a migration, adds query complexity, and raises questions about retention policy. Out of scope for now.

**Why this**: The proposal explicitly scopes messages as ephemeral. Persistence can be added later without changing the API contract.

### 3. ChatMessage domain model in Abstractions; handler in Game core

**Decision**: `ChatMessage` record goes in `CardCheesi.Game.Abstractions.DomainModels`; `SendChatMessageCommand` + handler in `CardCheesi.Game`; endpoint in `CardCheesi.Game.Api`. Follows the existing Vertical Slice / CQRS pattern used by `CreateGame`, `JoinGame`, `GetGame`.

### 4. Frontend: new `ChatPanelComponent` mirroring HUD layout

**Decision**: `ChatPanelComponent` follows the same slide animation and backdrop-blur pattern as `GameHudComponent` but anchored to the left edge and sliding horizontally. Toggle button on the right edge of the panel. Unread count badge on toggle when collapsed.

**Alternatives considered**:
- **Modal / dialog**: Intrusive, breaks game immersion.
- **Bottom-right corner drawer**: Conflicts spatially with the HUD.

### 5. SSE event carries full message payload

**Decision**: The `chat-message` SSE event payload includes `{ senderId, senderName, text, timestamp }`. The frontend does not need a follow-up fetch.

## Risks / Trade-offs

- **Fan-out at scale**: In-memory fan-out is fine for 4 players per game but won't scale across multiple server instances. Mitigation: acceptable for current scope; can be replaced with a pub/sub broker (e.g., Redis) in a future change.
- **Message loss on reconnect**: Players who disconnect and reconnect miss messages sent while they were offline (no history). Mitigation: documented as a known limitation; acceptable per Non-Goals.
- **XSS in chat text**: Frontend must HTML-encode message text before rendering. Angular's template binding does this automatically when using text interpolation (`{{ }}`), so raw `innerHTML` must not be used.
