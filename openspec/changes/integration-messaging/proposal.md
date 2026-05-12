## Why

The CardCheesi system consists of multiple independent services (Players API, Game API) that need to react to each other's domain events without direct coupling. Currently there is no event-driven communication between services, meaning cross-cutting concerns like player presence, game state changes, and player lifecycle events cannot be observed or acted upon by other services or future subscribers. Introducing a shared integration event library with a Dapr pub/sub backing provides a decoupled, observable, and extensible messaging backbone.

## What Changes

- Add a new shared library project `CardCheesi.IntegrationMessages` containing typed integration event records for all significant domain events across the system.
- Add Dapr pub/sub component to the Aspire AppHost using the Community Toolkit `AddDaprPubSub()` extension.
- Publish integration events from the Players API and Game API when key domain actions occur.
- Wire Dapr sidecar to both API services via the AppHost.

## Capabilities

### New Capabilities

- `integration-messages-library`: A new `CardCheesi.IntegrationMessages` class library defining all integration event records: `PlayerCreatedEvent`, `GameCreatedEvent`, `PlayerAddedToGameEvent`, `PlayerLeftGameEvent`, `PlayerWentOfflineEvent`, `PlayerCameOnlineEvent`.
- `dapr-pubsub-setup`: Dapr pub/sub component added to Aspire AppHost via the Community Toolkit; both Players API and Game API are wired with a Dapr sidecar; a topic-per-event-type convention is established.
- `publish-player-events`: Players API publishes `PlayerCreatedEvent`, `PlayerWentOfflineEvent`, `PlayerCameOnlineEvent` via Dapr pub/sub after successful domain actions.
- `publish-game-events`: Game API publishes `GameCreatedEvent`, `PlayerAddedToGameEvent`, `PlayerLeftGameEvent` via Dapr pub/sub after successful domain actions.

### Modified Capabilities

_(none — no existing spec-level requirements change)_

## Impact

- **New project**: `src/CardCheesi.IntegrationMessages/` — referenced by both API projects and potentially future consumers.
- **AppHost** (`src/card-cheesi.AppHost/`): adds Dapr Community Toolkit NuGet, calls `AddDaprPubSub()`, wires sidecars to `players-api` and `game-api` resources.
- **Players API** (`src/Players/CardCheesi.Players.Api/`): adds `Dapr.Client` dependency, publishes events after player registration and presence changes.
- **Game API** (`src/Game/CardCheesi.Game.Api/`): adds `Dapr.Client` dependency, publishes events after game creation and player join/leave.
- **No breaking changes** to existing HTTP API contracts.
