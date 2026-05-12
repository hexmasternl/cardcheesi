## 1. Integration Messages Library

- [ ] 1.1 Create project `src/CardCheesi.IntegrationMessages/CardCheesi.IntegrationMessages.csproj` targeting `net10.0` with no package dependencies
- [ ] 1.2 Add the project to `src/card-cheesi.slnx`
- [ ] 1.3 Define base fields convention: all events include `Guid EventId` and `DateTimeOffset OccurredAt`
- [ ] 1.4 Create `PlayerCreatedEvent` sealed record with fields: `PlayerId`, `PlayerName`, `EventId`, `OccurredAt`
- [ ] 1.5 Create `PlayerWentOfflineEvent` sealed record with fields: `PlayerId`, `PlayerName`, `GameCode`, `EventId`, `OccurredAt`
- [ ] 1.6 Create `PlayerCameOnlineEvent` sealed record with fields: `PlayerId`, `PlayerName`, `GameCode`, `EventId`, `OccurredAt`
- [ ] 1.7 Create `GameCreatedEvent` sealed record with fields: `GameId`, `GameCode`, `CreatorPlayerId`, `CreatorPlayerName`, `EventId`, `OccurredAt`
- [ ] 1.8 Create `PlayerAddedToGameEvent` sealed record with fields: `GameId`, `GameCode`, `PlayerId`, `PlayerName`, `EventId`, `OccurredAt`
- [ ] 1.9 Create `PlayerLeftGameEvent` sealed record with fields: `PlayerId`, `PlayerName`, `GameCode`, `EventId`, `OccurredAt`

## 2. Dapr Pub/Sub in Aspire AppHost

- [ ] 2.1 Add `CommunityToolkit.Aspire.Hosting.Dapr` NuGet package to `card-cheesi.AppHost.csproj`
- [ ] 2.2 Call `builder.AddDaprPubSub("pubsub")` in `AppHost.cs` to register the pub/sub component
- [ ] 2.3 Chain `.WithDaprSidecar()` on the `playersApi` resource in `AppHost.cs`
- [ ] 2.4 Chain `.WithDaprSidecar()` on the `gameApi` resource in `AppHost.cs`
- [ ] 2.5 Verify all resources start healthy in the Aspire dashboard after AppHost changes

## 3. Dapr Client in API Projects

- [ ] 3.1 Add `Dapr.AspNetCore` NuGet package to `CardCheesi.Players.Api.csproj`
- [ ] 3.2 Call `builder.Services.AddDaprClient()` in `CardCheesi.Players.Api/Program.cs`
- [ ] 3.3 Add project reference `CardCheesi.IntegrationMessages` to `CardCheesi.Players.Api.csproj`
- [ ] 3.4 Add `Dapr.AspNetCore` NuGet package to `CardCheesi.Game.Api.csproj`
- [ ] 3.5 Call `builder.Services.AddDaprClient()` in `CardCheesi.Game.Api/Program.cs`
- [ ] 3.6 Add project reference `CardCheesi.IntegrationMessages` to `CardCheesi.Game.Api.csproj`

## 4. Publish Player Events

- [ ] 4.1 Inject `DaprClient` into `RegisterPlayerHandler` constructor; add project reference `CardCheesi.IntegrationMessages` to `CardCheesi.Players.csproj`
- [ ] 4.2 After `_db.SaveChangesAsync` succeeds in `RegisterPlayerHandler`, publish `PlayerCreatedEvent` to topic `"player-created"` wrapped in try/catch that logs `Warning` on failure
- [ ] 4.3 Add project reference `CardCheesi.IntegrationMessages` to `CardCheesi.Game.csproj`
- [ ] 4.4 Inject `DaprClient` into `PlayerPresenceTracker`; publish `PlayerCameOnlineEvent` to `"player-came-online"` in `ConnectAsync` after `BroadcastStatusAsync`
- [ ] 4.5 Publish `PlayerWentOfflineEvent` to `"player-went-offline"` in `DisconnectAsync` after `BroadcastStatusAsync`
- [ ] 4.6 Publish `PlayerLeftGameEvent` to `"player-left-game"` inside the grace-period expiry branch of `DisconnectAsync`

## 5. Publish Game Events

- [ ] 5.1 Inject `DaprClient` into `CreateGameHandler`; publish `GameCreatedEvent` to `"game-created"` after `_repo.SaveAsync` succeeds, wrapped in try/catch logging `Warning` on failure
- [ ] 5.2 Inject `DaprClient` into `JoinGameHandler`; publish `PlayerAddedToGameEvent` to `"player-added-to-game"` after `_repo.SaveAsync` succeeds, wrapped in try/catch logging `Warning` on failure

## 6. Verification

- [ ] 6.1 Build the full solution (`dotnet build src/card-cheesi.slnx`) with zero errors
- [ ] 6.2 Run `dotnet test src/card-cheesi.slnx` and confirm all existing tests pass
- [ ] 6.3 Start the AppHost and verify all resources (including Dapr sidecars) reach healthy state in the dashboard
- [ ] 6.4 Register a player via `POST /api/players` and confirm `player-created` event appears in Dapr dashboard traces
- [ ] 6.5 Create a game and join with a second player; confirm `game-created` and `player-added-to-game` events appear
- [ ] 6.6 Connect and disconnect an SSE client; confirm `player-came-online` and `player-went-offline` events appear
