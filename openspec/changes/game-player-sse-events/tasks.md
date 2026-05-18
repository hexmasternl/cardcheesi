## 1. Backend — Broadcast player-joined event

- [x] 1.1 Inject `ISseConnectionManager` into `JoinGameHandler` and broadcast a `player-joined` SSE event (payload: `{ playerId, playerName }`) after a successful `_repo.SaveAsync` call
- [x] 1.2 Add a unit test to `JoinGameHandlerTests` verifying that `ISseConnectionManager.BroadcastAsync` is called with event type `player-joined` and the correct payload after a successful join
- [x] 1.3 Add a unit test verifying that `BroadcastAsync` is NOT called when the join fails (game full, already joined, not waiting)

## 2. Frontend — SseService: player-joined signal

- [x] 2.1 Add `PlayerJoinedEvent` interface (`{ playerId: string; playerName: string }`) to `sse.service.ts`
- [x] 2.2 Add `lastPlayerJoined` signal (`signal<PlayerJoinedEvent | null>(null)`) to `SseService`
- [x] 2.3 Register an `addEventListener('player-joined', ...)` listener on the `EventSource` in `SseService.connect()` that parses the payload and updates `lastPlayerJoined`
- [x] 2.4 Reset `lastPlayerJoined` to `null` in `SseService.disconnect()`
- [x] 2.5 Update `SseEventType` union to include `'player-joined'`

## 3. Frontend — GamePage: reactive game state

- [x] 3.1 In `GamePage`, add an `effect()` that watches `sseService.lastPlayerJoined()`; when a non-null event arrives, append the player to `gameState().players` (deduplicate by `id`)
- [x] 3.2 Ensure the effect is a no-op when `gameState()` is `null`

## 4. Frontend — PlayerPresenceStore: seed from player-joined

- [x] 4.1 In `PlayerPresenceStore`, add a second `effect()` that watches `sseService.lastPlayerJoined()`; when a non-null event arrives and the player is not already in `_presenceMap`, insert them with `status: 'Connected'`

## 5. Tests

- [x] 5.1 Write a unit test for `SseService` verifying that `lastPlayerJoined` is updated when a `player-joined` event is received and remains unchanged on a malformed payload
- [x] 5.2 Write a unit test for `PlayerPresenceStore` verifying that a `player-joined` event seeds a new player with `Connected` status and does not overwrite an existing entry
- [x] 5.3 Write a unit test for `GamePage` verifying that a `player-joined` event appends a new player and deduplicates correctly

## 6. Verification

- [x] 6.1 Build the .NET solution (`dotnet build src/card-cheesi.slnx`) and confirm zero errors
- [x] 6.2 Run all .NET tests (`dotnet test src/card-cheesi.slnx`) and confirm they pass
- [x] 6.3 Run Angular tests (`ng test` from `src/App/CardCheesi`) and confirm they pass
