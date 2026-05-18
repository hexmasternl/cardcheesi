## 1. Backend — GET /games/{code} endpoint

- [x] 1.1 Add `GET /games/{code}` endpoint to `Program.cs` using `IGameRepository.GetByCodeAsync`; return `200 GameState` or `404`
- [x] 1.2 Add a unit/integration test for the new endpoint: existing code returns 200, unknown code returns 404

## 2. Board render asset

- [x] 2.1 Export a high-quality render (1920×1080, WebP or PNG) of the board from `3dmodels/board.blend` and save to `src/App/src/assets/board-render.webp`

## 3. Game page component

- [x] 3.1 Create `src/app/pages/game/` directory and scaffold `game-page.ts` as a standalone Angular component
- [x] 3.2 Add the `/game/:gameCode` lazy-loaded route to `app.routes.ts`
- [x] 3.3 Style `game-page.scss` with full-screen `background-image: url('/assets/board-render.svg')`, `background-size: cover`, and a dark fallback colour
- [x] 3.4 Inject `ActivatedRoute` and read the `:gameCode` param via Signals on component init
- [x] 3.5 Create `GameService` (or inline) to call `GET /api/games/{code}` via `HttpClient` and return the `GameState` as an Observable/Signal
- [x] 3.6 Display a PrimeNG spinner while loading and a "Game not found" message with a home link on 404
- [x] 3.7 Display the list of players (`GameState.players`) with names using a PrimeNG component consistent with the CardCheesi theme
- [x] 3.8 Add a themed "Refresh" button that re-fetches the game state without a full page reload

## 4. Polish & verification

- [x] 4.1 Verify the background image covers the full viewport at 1280×800, 1920×1080, and mobile (375×812) breakpoints
- [x] 4.2 Confirm the theme colours, fonts, and component styles match the existing landing/rules pages
- [x] 4.3 Run `ng build` and confirm no TypeScript or build errors

## 5. Backend — Chat API

- [x] 5.1 Chat messages are delivered via the dedicated `CardCheesi.Chat.Api` SSE service at `/api/chat/{code}/events`; no ChatMessage entity needed in GameApi
- [x] 5.2 `SseService` connects to `/api/chat/{code}/events` and accumulates `chat-message` events into `chatMessages` signal
- [x] 5.3 Sending a message calls `POST /api/chat/{code}` via the Chat API
- [x] 5.4 Chat API service is already tested; no additional game-view tests required for chat

## 6. Backend — Move and Dispose endpoints

- [x] 6.1 `POST /games/{code}/move` endpoint implemented in `GameEndpoints.cs`; validates turn, applies move via `MakeMoveHandler`, broadcasts `game-updated` + `your-turn` SSE, returns `204`
- [x] 6.2 `POST /games/{code}/dispose` endpoint implemented in `GameEndpoints.cs`; validates turn, disposes hand via `DisposeHandHandler`, broadcasts `game-updated` + `your-turn` SSE, returns `204`
- [ ] 6.3 Add unit tests for `MakeMoveHandler`: game-not-found → `NotFoundException`; not-in-progress → `DomainException`; out-of-turn → `DomainException`; Jack missing second pawn → `DomainException`; valid move saves and broadcasts two SSE events
- [ ] 6.4 Add unit tests for `DisposeHandHandler`: game-not-found → `NotFoundException`; not-in-progress → `DomainException`; out-of-turn → `DomainException`; has-playable-cards → `DomainException`; valid dispose saves and broadcasts two SSE events

## 7. Frontend — Chat overlay component

- [x] 7.1 `ChatPanelComponent` created at `src/app/pages/game/chat-panel/chat-panel.ts`; anchored fixed to the right edge; collapsed by default
- [x] 7.2 Toggle button (`pi pi-comments` icon) expands/collapses the panel; unread badge increments on new messages while collapsed and clears on open
- [x] 7.3 Messages rendered from `sseService.chatMessages()` in chronological order with player name + timestamp; local player messages right-aligned in primary colour
- [x] 7.4 Text input + send button calls `POST /api/chat/{code}`; clears input on success; suppresses empty sends
- [x] 7.5 On mobile (< 600px) expands to full viewport width; `ChatPanelComponent` integrated into `GamePage` template

## 8. Frontend — Game control drawer component

- [x] 8.1 `GameHudComponent` created at `src/app/pages/game/game-hud/game-hud.ts`; persists local `playerId` in `localStorage` as `cardcheesi_player_{gameCode}`; reads it to determine if it is the local player's turn
- [x] 8.2 Drawer bottom-bar always visible (status text shows active player name); drawer slides up automatically when `turn.activePlayerId === localPlayerId`
- [x] 8.3 Local player's hand rendered as face-up playing card components; suit symbols with correct colouring; horizontal scrollable row
- [x] 8.4 Card selection signal implemented via `TurnFlowStore`; selected card highlighted; deselect on re-click; pawn selection resets on card change
- [x] 8.5 Pawn selection rules per card implemented in `TurnFlowStore`: standard, Jack (2 pawns), Seven with split counter, Ace choice modal
- [x] 8.6 Eligible pawns highlighted on board when card selected; ineligible pawns dimmed (via `selectablePawnIds` / `blinkingPawnIds` signals)
- [x] 8.7 "Play Move" button: disabled until card + valid pawn selection; on click calls `POST /api/games/{code}/move` → collapses drawer; SSE `game-updated` event triggers automatic game state refresh
- [x] 8.8 "Dispose Cards" button: disabled when any playable card exists; on click calls `POST /api/games/{code}/dispose` → collapses drawer; SSE `game-updated` triggers refresh
- [x] 8.9 `GameHudComponent` integrated into `GamePage` template; `ng build` passes with 0 errors

## 9. Frontend — Pawn position animation

- [ ] 9.1 Add `resolveWorldPosition(pawn, playerIndex, reserveIndex)` helper to `board-coordinates.ts` that delegates to `boardPositionToWorld`, `finishPositionToWorld`, or `RESERVE_POSITIONS` based on `pawn.location.$type`
- [ ] 9.2 Refactor `PawnLayer.spawnedPawns` from `SpawnedPawn[]` to `Map<string, SpawnedPawn>` keyed by pawn ID
- [ ] 9.3 Add `movePawns(players, status, blinking, selectable)` method to `PawnLayer` that spawns missing pawns, removes stale ones, and uses `Animation.CreateAndStartAnimation` to animate existing pawns to new positions (30 fps, 15 frames)
- [ ] 9.4 Update `GameBoardComponent` to call `placePawns()` on first render and `movePawns()` on subsequent state changes
- [ ] 9.5 Add unit tests for `resolveWorldPosition` covering board, finish, and reserve location types
