## ADDED Requirements

### Requirement: Aspire AppHost provisions a Dapr pub/sub component
The Aspire AppHost SHALL add a Dapr pub/sub component named `"pubsub"` using the `AddDaprPubSub()` extension method from the `CommunityToolkit.Aspire.Hosting.Dapr` package.

#### Scenario: Pub/sub component is available at startup
- **WHEN** the AppHost starts
- **THEN** a Dapr pub/sub component named `"pubsub"` SHALL be registered and visible in the Aspire dashboard

### Requirement: Both API services are wired with a Dapr sidecar
The Players API and the Game API resources in the AppHost SHALL each have a Dapr sidecar attached via `.WithDaprSidecar()`. Each API service SHALL wait for its sidecar to be ready before accepting traffic.

#### Scenario: Players API has a Dapr sidecar
- **WHEN** the AppHost starts
- **THEN** the `cardcheesi-players-api` resource SHALL have a running Dapr sidecar

#### Scenario: Game API has a Dapr sidecar
- **WHEN** the AppHost starts
- **THEN** the `cardcheesi-game-api` resource SHALL have a running Dapr sidecar

### Requirement: Dapr component name convention is established
The pub/sub component SHALL be named `"pubsub"` (the Dapr default). All publishers SHALL use this name when calling `DaprClient.PublishEventAsync`.

#### Scenario: Publisher uses correct component name
- **WHEN** an integration event is published
- **THEN** `DaprClient.PublishEventAsync("pubsub", topicName, eventPayload)` is called with component name `"pubsub"`

### Requirement: Topic naming follows kebab-case event-type convention
Each integration event type SHALL be published to a dedicated topic named in `kebab-case` matching the event type: `player-created`, `game-created`, `player-added-to-game`, `player-left-game`, `player-went-offline`, `player-came-online`.

#### Scenario: Event is published to correct topic
- **WHEN** a `PlayerCreatedEvent` is published
- **THEN** the Dapr topic used SHALL be `"player-created"`
