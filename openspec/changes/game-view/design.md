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
- Real-time updates (WebSocket/SignalR) — polling or manual refresh only for now
- Card play interactions or move logic — display only
- 3D re-rendering in the browser (no Three.js); a static pre-rendered image is sufficient

## Decisions

### 1. Static pre-rendered board image vs. real-time 3D
**Decision:** Use a static PNG/WebP exported from Blender as the background.  
**Rationale:** The `board.blend` asset already exists. Embedding a Three.js renderer introduces significant bundle size and complexity with no gameplay benefit at this stage. A high-quality render at 1920×1080 is indistinguishable from a live 3D scene as a background.  
**Alternative considered:** Three.js with OrbitControls — deferred to a future iteration.

### 2. Game state fetching strategy
**Decision:** Fetch once on component init via `HttpClient`; expose a manual refresh button.  
**Rationale:** Real-time play requires WebSocket/SSE infrastructure not yet in place. A simple HTTP `GET` on load is sufficient to display the current state and lets real-time sync be layered in later without changing the component contract.  
**Alternative considered:** Server-Sent Events — deferred.

### 3. Component structure
**Decision:** A single `GamePage` container component at `src/app/pages/game/game-page.ts` with inline sub-components (`GameBoardComponent`, `PlayerHandComponent`, `GameStatusComponent`) co-located in `src/app/pages/game/`.  
**Rationale:** Mirrors the existing pages pattern (`landing/`, `rules/`). Keeps game-specific components scoped to the page until they are needed elsewhere.

### 4. Backend endpoint
**Decision:** Add `GET /games/{code}` returning `200 GameState` or `404` to `Program.cs` using the existing `IGameRepository.GetByCodeAsync`.  
**Rationale:** The repository already exposes this method. No new infrastructure needed.

### 5. Chat overlay persistence
**Decision:** Chat messages are persisted in a dedicated `ChatMessages` table in PostgreSQL; fetched via `GET /api/games/{code}/chat` and sent via `POST /api/games/{code}/chat`.  
**Rationale:** Storing chat server-side means messages survive browser refreshes and are visible to all players. Using the existing Postgres connection avoids a new dependency. Real-time delivery is deferred — chat updates piggyback on the existing game-state refresh cycle.  
**Alternative considered:** localStorage-only — rejected because messages would not be shared across players.

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

## Risks / Trade-offs

- **3D image must be exported manually** — The `.blend` file exists but a rendered PNG is not yet in `src/assets/`. The board render must be created and committed before the frontend task completes. → Mitigation: document the export step in tasks; use a placeholder image until the render is ready.
- **Stale game state** — Without real-time updates, the displayed state may lag. → Mitigation: add a visible "Refresh" action; acceptable for the current development stage.
- **Route guard missing** — `/game/:gameCode` with a non-existent code returns a 404 from the API. → Mitigation: show a user-friendly error state in the component when the API returns 404.
