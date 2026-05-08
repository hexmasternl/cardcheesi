## 1. Backend — GET /games/{code} endpoint

- [x] 1.1 Add `GET /games/{code}` endpoint to `Program.cs` using `IGameRepository.GetByCodeAsync`; return `200 GameState` or `404`
- [x] 1.2 Add a unit/integration test for the new endpoint: existing code returns 200, unknown code returns 404

## 2. Board render asset

- [ ] 2.1 Export a high-quality render (1920×1080, WebP or PNG) of the board from `3dmodels/board.blend` and save to `src/App/src/assets/board-render.webp`

## 3. Game page component

- [ ] 3.1 Create `src/app/pages/game/` directory and scaffold `game-page.ts` as a standalone Angular component
- [ ] 3.2 Add the `/game/:gameCode` lazy-loaded route to `app.routes.ts`
- [ ] 3.3 Style `game-page.scss` with full-screen `background-image: url('/assets/board-render.webp')`, `background-size: cover`, and a dark fallback colour
- [ ] 3.4 Inject `ActivatedRoute` and read the `:gameCode` param via Signals on component init
- [ ] 3.5 Create `GameService` (or inline) to call `GET /api/games/{code}` via `HttpClient` and return the `GameState` as an Observable/Signal
- [ ] 3.6 Display a PrimeNG spinner while loading and a "Game not found" message with a home link on 404
- [ ] 3.7 Display the list of players (`GameState.players`) with names using a PrimeNG component consistent with the CardCheesi theme
- [ ] 3.8 Add a themed "Refresh" button that re-fetches the game state without a full page reload

## 4. Polish & verification

- [ ] 4.1 Verify the background image covers the full viewport at 1280×800, 1920×1080, and mobile (375×812) breakpoints
- [ ] 4.2 Confirm the theme colours, fonts, and component styles match the existing landing/rules pages
- [ ] 4.3 Run `ng build` and confirm no TypeScript or build errors
