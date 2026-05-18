## Context

The CardCheesi frontend is an Angular 21 SPA with standalone components, Signals for state, and PrimeNG/Aura theme. Two routes exist today: `/` (landing) and `/rules`. There is no in-game view.

The backend already persists game state as JSONB in PostgreSQL via `IGameRepository`. The `GameState` model includes players, teams, pawns, turn state, deck, and hands. A `GET /games/{code}` endpoint needs to be added so the frontend can fetch game state by code.

The `3dmodels/board.blend` file is the Blender source for the game board. A pre-rendered image needs to be exported and placed in `src/assets/` for use as the background.

## Goals / Non-Goals

**Goals:**
- Add `/game/:gameCode` Angular route rendering `GamePage` from `src/app/pages/game/`
- Full-screen 3D board render as the page background (CSS `background-image`)
- Display current game state: player list, pawn positions, and (if in-progress) the local player's hand
- Add `GET /games/{code}` API endpoint returning the current `GameState` as JSON
- Consistent with the CardCheesi theme (primary `#009ccc`, dark background, PrimeNG components)

**Non-Goals:**
- 3D re-rendering in the browser (no Three.js); a static pre-rendered image is sufficient for the board background
- WebSocket-based real-time updates — SSE (Server-Sent Events) is already in place

## Decisions

### 1. Static pre-rendered board image vs. real-time 3D
**Decision:** Use a static PNG/WebP exported from Blender as the background.  
**Rationale:** The `board.blend` asset already exists. Embedding a Three.js renderer introduces significant bundle size and complexity with no gameplay benefit at this stage. A high-quality render at 1920×1080 is indistinguishable from a live 3D scene as a background.  
**Alternative considered:** Three.js with OrbitControls — deferred to a future iteration.

### 2. Game state fetching strategy
**Decision:** Fetch on component init and on every `game-updated` SSE event via `HttpClient`.  
**Rationale:** The `SseService` already emits a `lastGameUpdated` signal each time the server broadcasts `game-updated`. `GamePage` uses an Angular `effect()` to call `fetchGame()` whenever that counter increments. This makes state refresh automatic and real-time after any move or dispose action.  
**Alternative considered:** Polling interval — rejected; SSE push is more efficient and already in place.

### 3. Component structure
**Decision:** A single `GamePage` container component at `src/app/pages/game/game-page.ts` with inline sub-components (`GameBoardComponent`, `PlayerHandComponent`, `GameStatusComponent`) co-located in `src/app/pages/game/`.  
**Rationale:** Mirrors the existing pages pattern (`landing/`, `rules/`). Keeps game-specific components scoped to the page until they are needed elsewhere.

### 4. Backend endpoint
**Decision:** Add `GET /games/{code}` returning `200 GameState` or `404` to `Program.cs` using the existing `IGameRepository.GetByCodeAsync`.  
**Rationale:** The repository already exposes this method. No new infrastructure needed.

### 5. Chat overlay persistence
**Decision:** Chat messages are delivered via SSE through the dedicated `CardCheesi.Chat.Api` service. The frontend `SseService` connects to `/api/chat/{code}/events` and accumulates `chat-message` events into a `chatMessages` signal. Sending a message calls `POST /api/chat/{code}`.  
**Rationale:** The Chat API is a separate Aspire-managed service (`/api/chat/**` is YARP-proxied to `chatApi`). SSE delivery avoids polling, and the chat events arrive alongside game events via two parallel `EventSource` connections in `SseService`.

### 6. Game control drawer — local player identity
**Decision:** The local player's `playerId` (returned by `POST /games` and `POST /games/{code}/join`) is stored in `localStorage` keyed by game code (`cardcheesi_player_{gameCode}`). The drawer reads this value to decide whether it is the local player's turn.  
**Rationale:** The app has no authentication at this stage. `localStorage` is the simplest way to persist identity across browser refreshes without introducing a user session.  
**Alternative considered:** URL-based player token — deferred; adds complexity to deep-linking.

### 7. Client-side move validation for "Dispose Cards"
**Decision:** The "Dispose Cards" button enabled-state is determined client-side by evaluating card playability against current pawn positions.  
**Rationale:** Avoids an extra API round-trip; the full `GameState` (pawn locations, hand) is already loaded. A server-side `400` response handles edge cases where the client-side check is wrong.  
**Alternative considered:** A `GET /games/{code}/canplay` endpoint — overkill for this stage.

### 8. Seven-card split UX
**Decision:** When a Seven is played with two pawns selected, a single counter control is shown for pawn A (1–6 range); pawn B's steps are auto-calculated as `7 − A`.  
**Rationale:** Eliminates the need for the user to manually balance two counters; a single slider/stepper is sufficient and intuitive.

### 9. SSE broadcast after move / dispose
**Decision:** `MakeMoveHandler` and `DisposeHandHandler` each broadcast two SSE events after persisting the new state: (1) `game-updated` with `{}` payload — triggers all connected clients to re-fetch game state; (2) `your-turn` with `{ activePlayerId, canDispose }` — signals the next player to open the HUD.  
**Rationale:** All clients subscribe to `/api/games/{code}/events` via `EventSource`. Broadcasting `game-updated` after each mutation keeps every player's view in sync without polling. The separate `your-turn` event lets the HUD expand automatically for the next player without requiring them to re-fetch first.

### 10. Pawn position animation
**Decision:** When a `game-updated` event arrives, the frontend re-fetches the game state and passes the new positions to `GameBoardComponent`. `PawnLayer` maintains a `Map<string, SpawnedPawn>` registry keyed by pawn ID. On subsequent updates `movePawns()` reuses existing mesh instances and animates each pawn's `position` property from its current coordinates to the new target using `Animation.CreateAndStartAnimation()` at 30 fps over 15 frames (500 ms).  
**Rationale:** Smooth movement (rather than teleportation) gives clear visual feedback about which pawns moved and how far. Reusing mesh instances via the registry avoids unnecessary dispose/respawn cycles and keeps ActionManagers intact across moves. The 15-frame duration is long enough to be visible but short enough not to impede gameplay flow.

## Risks / Trade-offs

- **3D image must be exported manually** — The `.blend` file exists but a rendered PNG is not yet in `src/assets/`. The board render must be created and committed before the frontend task completes. → Mitigation: document the export step in tasks; use a placeholder image until the render is ready.
- **Route guard missing** — `/game/:gameCode` with a non-existent code returns a 404 from the API. → Mitigation: show a user-friendly error state in the component when the API returns 404.
