## 1. Backend — SSE infrastructure

- [x] 1.1 Add `PlayerPresenceStatus` enum (`Connected`, `Disconnected`, `Left`) and `PlayerPresenceEvent` record (`PlayerId`, `PlayerName`, `Status`) to `CardCheesi.Game.Abstractions`
- [x] 1.2 Create `SseEvent` record (`EventType`, `Data`) and `ISseConnectionManager` interface (`AddConnection`, `RemoveConnection`, `BroadcastAsync`) in `CardCheesi.Game.Abstractions`
- [x] 1.3 Implement `SseConnectionManager` in `CardCheesi.Game` using `ConcurrentDictionary<string, List<Channel<SseEvent>>>` keyed by game code; implement `BroadcastAsync` to fan out to all channels for a game
- [x] 1.4 Register `ISseConnectionManager` as a singleton in `CardCheesi.Game.Api` DI container
- [x] 1.5 Add `GET /games/{code}/events` endpoint to `Program.cs`: set `Content-Type: text/event-stream`, `Cache-Control: no-cache`, `X-Accel-Buffering: no`; return 404 if game not found; create a `Channel<SseEvent>`, register with `SseConnectionManager`, stream events to response body until cancellation; deregister channel on completion
- [x] 1.6 Add a keep-alive background loop inside the SSE endpoint handler: write `: keep-alive\n\n` every 15 seconds using a `PeriodicTimer`

## 2. Backend — Player presence tracking

- [x] 2.1 Create `PlayerPresenceTracker` service in `CardCheesi.Game` that holds per-game, per-player presence state (`ConcurrentDictionary`); expose `ConnectAsync(gameCode, playerId, playerName)` and `DisconnectAsync(gameCode, playerId)` methods
- [x] 2.2 `ConnectAsync`: if a pending disconnect timer exists for the player, cancel it; set status to `Connected`; broadcast `player-status` event via `ISseConnectionManager`
- [x] 2.3 `DisconnectAsync`: set status to `Disconnected`; broadcast `player-status` event; start a 30-second `Task.Delay` grace timer; on expiry set status to `Left` and broadcast final `player-status` event (unless already `Connected` again)
- [x] 2.4 Register `PlayerPresenceTracker` as a singleton in DI
- [x] 2.5 Update `GET /games/{code}/events` endpoint: after registering the channel, call `PlayerPresenceTracker.ConnectAsync`; send initial presence snapshot (one `player-status` event per player in the game) before entering the event loop; call `PlayerPresenceTracker.DisconnectAsync` in the finally block

## 3. Backend — Tests

- [x] 3.1 Unit test `SseConnectionManager`: broadcasting sends events to all registered channels for a game; channels for other games are not notified; removing a connection stops delivery
- [x] 3.2 Unit test `PlayerPresenceTracker`: connect sets status to `Connected` and broadcasts; disconnect sets status to `Disconnected` and starts grace timer; reconnect within 30 s cancels timer and restores `Connected`; grace period expiry transitions to `Left`
- [x] 3.3 Integration test for `GET /games/{code}/events`: unknown code returns 404; known code returns 200 with `text/event-stream`; initial snapshot events are sent on connect

## 4. Frontend — SSE service

- [x] 4.1 Create `SseService` at `src/app/services/sse.service.ts` as an injectable singleton; wraps `EventSource`; exposes `connect(gameCode, playerId)` and `disconnect()` methods; returns a typed `Observable<SseEvent>` or exposes a `Signal<SseEvent | null>`
- [x] 4.2 `SseService.connect`: open `EventSource` to `/api/games/{code}/events?playerId={id}`; listen for named event `player-status` and emit typed payloads; handle `EventSource.onerror` and surface an error signal
- [x] 4.3 `SseService.disconnect`: close the `EventSource` connection

## 5. Frontend — Player presence panel

- [x] 5.1 Create `PlayerPresencePanelComponent` at `src/app/pages/game/player-presence-panel/player-presence-panel.ts` as a standalone `OnPush` component
- [x] 5.2 Panel SCSS: fixed position `top: 1rem; left: 1rem`; semi-transparent dark background (`rgba` using `_variables.scss` tokens); `z-index` above the board canvas; rounded corners; min-width 180px
- [x] 5.3 Component logic: inject `SseService`; maintain a `Signal<Map<string, PlayerPresenceStatus>>` indexed by `playerId`; update the map on each incoming `player-status` event
- [x] 5.4 Template: iterate over players from `GameState`; render each player as a row with a PrimeNG `Tag` component showing a coloured status badge (green/amber/red) and the player's name
- [x] 5.5 Status badge colours: use SCSS variables — no hardcoded hex values; `Connected` → `$color-success`, `Disconnected` → `$color-warning`, `Left` → `$color-danger`
- [x] 5.6 Integrate `PlayerPresencePanelComponent` into `GamePage` template; pass `GameState.players` as an input signal; call `SseService.connect` in `afterNextRender` and `SseService.disconnect` via `DestroyRef`

## 6. Polish & verification

- [x] 6.1 Run `ng build` from `src/App/CardCheesi` and confirm zero TypeScript / build errors
- [x] 6.2 Run `dotnet build src/card-cheesi.slnx` and confirm zero build errors
- [x] 6.3 Run `dotnet test src/card-cheesi.slnx` and confirm all tests pass
- [ ] 6.4 Manually verify: open two browser tabs for the same game; close one tab; confirm the other tab's panel shows `Disconnected` then `Left` after ~30 seconds
