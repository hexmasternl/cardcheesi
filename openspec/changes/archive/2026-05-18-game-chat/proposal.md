## Why

Players in an active game have no way to communicate with each other mid-game. A per-game chat panel lets players coordinate, react to moves, and stay engaged — using the SSE infrastructure already in place for real-time delivery.

## What Changes

- New `ChatMessage` domain model: game ID, sender player ID, sender name, text, and timestamp.
- New HTTP `POST /games/{code}/chat` endpoint to submit a chat message.
- New SSE event `chat-message` broadcast to all connected players in the game.
- New `ChatPanelComponent` on the frontend: expandable panel sliding in from the left side of the game page (mirrors the HUD pattern on the right/bottom).
- Chat panel shows a scrollable message list with sender name, message text, and timestamp.
- Chat panel includes a text input and send button at the bottom.
- Unread message badge on the toggle button when the panel is collapsed.

## Capabilities

### New Capabilities

- `game-chat`: Per-game chat — sending messages, real-time broadcast via SSE, and displaying the chat panel in the game UI.

### Modified Capabilities

- `game-sse`: Existing SSE stream gains a new `chat-message` event type emitted to all players in a game when a chat message is posted.

## Impact

- **Backend**: New `ChatMessage` domain model in `CardCheesi.Game.Abstractions`; new `SendChatMessageCommand` + handler in `CardCheesi.Game`; new endpoint in `CardCheesi.Game.Api`; `SseGameEventService` extended to broadcast `chat-message`.
- **Frontend**: New `ChatPanelComponent` standalone component; `game-page.ts` gains a `chatMessages` signal and SSE listener; `sse.service.ts` gains `ChatMessageEvent` and `chatMessages` signal.
- **Storage**: Chat messages are ephemeral (in-memory per game session, not persisted to DB).
- **Dependencies**: No new packages required.
