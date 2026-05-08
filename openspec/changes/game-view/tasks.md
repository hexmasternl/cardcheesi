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

- [ ] 5.1 Add `ChatMessage` entity to `AppDbContext` with fields: `Id` (Guid), `GameCode` (string), `PlayerId` (Guid), `PlayerName` (string), `Text` (string), `SentAt` (DateTimeOffset); add EF migration
- [ ] 5.2 Add `GET /games/{code}/chat` endpoint returning `ChatMessage[]` ordered by `SentAt` ascending
- [ ] 5.3 Add `POST /games/{code}/chat` endpoint accepting `{ playerId, text }`, validates text is non-empty, resolves `PlayerName` from the game, persists and returns `201 ChatMessage`
- [ ] 5.4 Add integration tests: fetch empty chat returns `[]`; posting a message stores it; posting to unknown game returns `404`

## 6. Backend — Move and Dispose endpoints

- [ ] 6.1 Add `POST /games/{code}/move` endpoint accepting `PlayMoveRequest { PlayerId, Card, PawnIds, SevenSplit? }`; validates it is the player's turn; applies move to `GameState`; persists; returns `204` or `400` with error detail
- [ ] 6.2 Add `POST /games/{code}/dispose` endpoint accepting `DisposeCardsRequest { PlayerId }`; validates it is the player's turn; removes the player's hand cards; advances turn; persists; returns `204` or `400`
- [ ] 6.3 Add integration tests: valid move returns `204` and updates state; out-of-turn move returns `400`; dispose returns `204` and advances `turn.activePlayerId`

## 7. Frontend — Chat overlay component

- [ ] 7.1 Create `src/app/pages/game/chat-overlay/chat-overlay.ts` standalone component; anchored fixed to the right edge; collapsed by default
- [ ] 7.2 Toggle button (`pi pi-comments` icon) expands/collapses the panel with a CSS slide transition; unread badge increments on new messages while collapsed and clears on open
- [ ] 7.3 Fetch `GET /api/games/{code}/chat` on game state refresh; render messages in chronological order with player name + timestamp; local player messages are right-aligned in primary colour
- [ ] 7.4 Add text input + send button at panel bottom; on submit call `POST /api/games/{code}/chat`; clear input on success; suppress empty sends
- [ ] 7.5 On mobile (< 600px) expand to full viewport width; integrate `ChatOverlay` into `GamePage` template

## 8. Frontend — Game control drawer component

- [ ] 8.1 Create `src/app/pages/game/game-control-drawer/game-control-drawer.ts`; persist local `playerId` in `localStorage` as `cardcheesi_player_{gameCode}` when game is joined/created; read it in the drawer to determine if it is the local player's turn
- [ ] 8.2 Drawer bottom-bar is always visible (status text shows active player name); drawer slides up automatically when `turn.activePlayerId === localPlayerId`
- [ ] 8.3 Render the local player's hand as face-up playing card components; suit symbols (♠ ♥ ♦ ♣) with correct colouring; horizontal scrollable row
- [ ] 8.4 Implement card selection signal; selected card is highlighted; deselect on re-click; pawn selection resets on card change
- [ ] 8.5 Implement pawn selection rules per card: 1 pawn for standard cards; 2 different-team pawns for Jack; 1–2 pawns for Seven with split counter (pawn A: 1–6 stepper, pawn B: auto = 7 − A); Ace choice modal (enter vs +1)
- [ ] 8.6 Highlight eligible pawns on the board when a card is selected; dim ineligible pawns
- [ ] 8.7 "Play Move" button in drawer top bar: disabled until card + valid pawn selection satisfied; on click send `POST /api/games/{code}/move` → collapse drawer → refresh game state
- [ ] 8.8 "Dispose Cards" button in drawer top bar: disabled when any playable card exists (client-side check); on click send `POST /api/games/{code}/dispose` → collapse drawer → refresh game state
- [ ] 8.9 Integrate `GameControlDrawer` into `GamePage` template; run `ng build` with 0 errors
