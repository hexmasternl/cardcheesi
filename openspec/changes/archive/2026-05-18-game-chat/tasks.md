## 1. Domain Model

- [x] 1.1 Add `ChatMessage` record to `CardCheesi.Game.Abstractions/DomainModels/ChatMessage.cs` with properties: `string GameCode`, `string SenderId`, `string SenderName`, `string Text`, `DateTimeOffset Timestamp`
- [x] 1.2 Add `ChatMessageDto` record to `CardCheesi.Game.Abstractions/DomainModels/ChatMessageDto.cs` for SSE/API response shape: `string SenderId`, `string SenderName`, `string Text`, `DateTimeOffset Timestamp`

## 2. Backend — Send Chat Message Command

- [x] 2.1 Create `SendChatMessageCommand` record in `CardCheesi.Game/Features/Chat/SendChatMessageCommand.cs` with `string GameCode`, `string SenderId`, `string Text`
- [x] 2.2 Create `SendChatMessageHandler` in `CardCheesi.Game/Features/Chat/SendChatMessageHandler.cs` — validates non-empty text (max 500 chars), checks game exists and player is a member, publishes `ChatMessage` via `ISseEventPublisher`, returns `Result<ChatMessageDto>`
- [x] 2.3 Add `ISseEventPublisher` abstraction to `CardCheesi.Game.Abstractions/Services/ISseEventPublisher.cs` with `void Publish(string gameCode, string eventName, object payload)` — or reuse/extend existing SSE infrastructure if a suitable interface already exists
- [x] 2.4 Register `SendChatMessageHandler` in DI (ensure it's picked up by the handler registration pattern used in the project)

## 3. Backend — API Endpoint

- [x] 3.1 Add `POST /games/{code}/chat` minimal API endpoint in `CardCheesi.Game.Api/Endpoints/Games/GameEndpoints.cs` (or a new `ChatEndpoints.cs`) — reads `{ text }` from body, resolves player identity from JWT claims, dispatches `SendChatMessageCommand`, returns 200/400/403/404 appropriately

## 4. Backend — SSE Broadcast

- [x] 4.1 Extend `SseGameEventService` (or `ISseEventPublisher` implementation) to accept `chat-message` events and write them to all open SSE response streams for the given game code
- [x] 4.2 Ensure the in-memory fan-out channel is keyed by game code and that concurrent writers are safe (`ConcurrentDictionary` or equivalent)

## 5. Backend — Tests

- [x] 5.1 Add unit tests for `SendChatMessageHandler`: valid message succeeds, empty text returns validation error, text > 500 chars returns validation error, non-member returns forbidden, unknown game returns not-found
- [x] 5.2 Add unit test verifying `ChatMessage` is published via `ISseEventPublisher` when a valid message is sent

## 6. Frontend — SSE Service Extension

- [x] 6.1 Add `ChatMessageEvent` interface to `sse.service.ts`: `{ senderId: string; senderName: string; text: string; timestamp: string }`
- [x] 6.2 Add `chatMessages` signal (`signal<ChatMessageEvent[]>([])`) to `SseService` and append to it when a `chat-message` SSE event is received
- [x] 6.3 Reset `chatMessages` to `[]` in `disconnect()`

## 7. Frontend — Chat Panel Component

- [x] 7.1 Create `src/app/pages/game/chat-panel/chat-panel.ts` — standalone, OnPush, `model()` for `expanded`, `messages` input, `sendMessage` output; `unreadCount` computed from messages received while collapsed; `inputText` signal for the text field
- [x] 7.2 Create `chat-panel.html` — toggle button with unread badge, scrollable message list (own messages right-aligned), composer input + send button at bottom
- [x] 7.3 Create `chat-panel.scss` — left-side slide-in animation (`translateX(-100%)` → `translateX(0)`), backdrop blur, toggle button tab on right edge, own-message vs other-message bubble styles, unread badge
- [x] 7.4 Implement auto-scroll to bottom when panel is expanded and a new message is appended (use `afterRender` or `effect` + `ElementRef` to call `scrollIntoView`)
- [x] 7.5 Wire Enter-key submission: bind `(keydown.enter)` on the input to the send handler; disable send button when input is empty/whitespace

## 8. Frontend — Game Page Integration

- [x] 8.1 Import `ChatPanelComponent` in `game-page.ts`; add `chatPanelExpanded` signal (`signal(false)`) and `chatMessages` computed from `SseService.chatMessages`
- [x] 8.2 Add `<app-chat-panel>` to `game-page.html` as a sibling of `<app-game-hud>`, positioned on the left side
- [x] 8.3 Implement `onSendMessage(text: string)` in `game-page.ts` — calls `POST /games/{code}/chat` via `HttpClient`; handle errors gracefully (toast or console)

## 9. Commit & Verify

- [x] 9.1 Trigger Aspire rebuild and confirm `Build succeeded`
- [x] 9.2 Run `ng build` from `src/App/CardCheesi/` and confirm zero errors
- [x] 9.3 Run `dotnet test` and confirm all tests pass (≥ 92 + new chat handler tests)
- [x] 9.4 Commit all changes with message `feat(game,frontend): add per-game chat with SSE broadcast and chat panel`
