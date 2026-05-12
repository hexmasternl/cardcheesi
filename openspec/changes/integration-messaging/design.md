## Context

CardCheesi is a distributed system with two independent APIs — Players API and Game API — each backed by its own PostgreSQL schema. Currently, these services have no awareness of each other's domain events. The frontend relies on SSE for real-time game presence, but there is no durable, cross-service event stream.

As features like player lifecycle (creation, online/offline toggling), game lifecycle (creation, players joining/leaving), and future services (e.g., leaderboards, spectator mode, audit log) grow, a dedicated integration event backbone becomes necessary. Rather than direct HTTP calls between services (tight coupling), we adopt Dapr pub/sub backed by a Redis streams component in development (provided by Aspire Community Toolkit). This is the standard cloud-native decoupled approach for .NET Aspire services.

**Current state:**
- Players API: `RegisterPlayerHandler` creates players, `PlayerCleanupService` ages out stale players.
- Game API: `CreateGameHandler`, `JoinGameHandler` mutate game state; `PlayerPresenceTracker` tracks in-memory SSE presence.
- No integration events are published anywhere today.

## Goals / Non-Goals

**Goals:**
- Define a shared `CardCheesi.IntegrationMessages` library with all integration event records.
- Provision a Dapr pub/sub component in the Aspire AppHost.
- Publish typed integration events from Players API and Game API handlers after successful domain actions.
- Establish a consistent topic-per-event-type naming convention.
- Keep the library dependency-free (plain C# records only) so any future subscriber can reference it without pulling in web or infrastructure packages.

**Non-Goals:**
- Implementing subscribers / consumers in this change (future work).
- Replacing SSE presence tracking (SSE remains the real-time mechanism for the frontend).
- Using MediatR or any in-process event bus — this is strictly for out-of-process integration events.
- Schema registry or event versioning strategy (deferred to a future ADR).
- Production Dapr component configuration (Redis/Kafka/Service Bus selection) — only the dev component is configured here.

## Decisions

### D1: Shared `CardCheesi.IntegrationMessages` library

A single, lightweight class library referenced by all publishing services (and eventually subscribers). It contains only `sealed record` event types — no dependencies on web frameworks, Dapr SDK, or persistence.

**Alternatives considered:**
- *Duplicate event types per service*: Ruled out — leads to drift and serialization mismatches.
- *Put events in Abstractions projects*: Ruled out — integration events are cross-module, not module-internal contracts.

### D2: Dapr pub/sub via Aspire Community Toolkit

Use `CommunityToolkit.Aspire.Hosting.Dapr` to call `AddDaprPubSub("pubsub")` in the AppHost, which provisions a Redis streams-backed pub/sub component locally. Both API projects receive a Dapr sidecar via `.WithDaprSidecar()`.

The Dapr component name `"pubsub"` is the conventional default and matches what `DaprClient.PublishEventAsync` uses.

**Alternatives considered:**
- *Custom `components/pubsub.yaml`*: More control but more boilerplate; the toolkit handles this automatically for local dev.
- *MassTransit / NServiceBus*: Heavier frameworks; Dapr is already the chosen orchestration model for this project.

### D3: Topic naming — one topic per event type

Each integration event is published to a dedicated topic named after its type in `kebab-case`:
- `player-created`, `game-created`, `player-added-to-game`, `player-left-game`, `player-went-offline`, `player-came-online`

**Rationale:** Subscribers can subscribe to exactly the events they care about without filtering. Fine-grained topics are idiomatic for Dapr pub/sub.

### D4: Publishing location — inside existing handlers, injected `DaprClient`

`DaprClient` is injected into the relevant handlers. After the successful `await _db.SaveChangesAsync()` / `await _repo.SaveAsync()`, the handler calls `await _daprClient.PublishEventAsync(...)`.

**Rationale:** Keeps publish logic co-located with the action that caused the event, avoids a separate outbox pattern for now. The known trade-off is that a publish failure after a successful DB save is not automatically retried — mitigated by Dapr's built-in at-least-once delivery when the component is available.

**Alternatives considered:**
- *Outbox pattern*: More reliable but significantly more complex; deferred to a future change if reliability requirements increase.
- *Domain events dispatched via middleware*: Cleaner but requires a more complex pipeline; out of scope here.

### D5: Presence events from `PlayerPresenceTracker`

`PlayerWentOfflineEvent` and `PlayerCameOnlineEvent` are published from `PlayerPresenceTracker.DisconnectAsync` and `ConnectAsync` respectively (in the Game API), since presence tracking lives there.

## Risks / Trade-offs

- **Publish-after-save is not atomic** → If `PublishEventAsync` fails after DB save, the event is lost. *Mitigation*: Log the failure; implement outbox pattern in a future change if event loss is unacceptable.
- **Dapr sidecar cold-start** → On first startup the sidecar may not be ready. *Mitigation*: Aspire health checks and `WaitFor` on the Dapr component ensure ordering.
- **Dapr Community Toolkit version compatibility** → The toolkit targets a specific Aspire SDK version. *Mitigation*: Pin compatible versions; verify at build time.
- **Redis not persisted by default** → Dev pub/sub events are ephemeral. *Mitigation*: Acceptable for local dev; production components (e.g., Azure Service Bus) will be configured later.

## Migration Plan

1. Add `CardCheesi.IntegrationMessages` project to solution — no impact on existing services.
2. Add Dapr Community Toolkit to AppHost — no impact on existing routes or APIs.
3. Add `DaprClient` injection to handlers — behind existing handler interfaces; no API contract changes.
4. Rebuild and restart services via Aspire; verify events appear in Dapr dashboard.

Rollback: Remove `DaprClient` calls from handlers and disable Dapr sidecars in AppHost.

## Open Questions

- Should `PlayerWentOfflineEvent` and `PlayerCameOnlineEvent` also be published from the Players API (based on `LastSeenAt` cleanup)? For now: **no** — presence is tracked per-game in the Game API.
- Should the topic prefix include a namespace (e.g., `cardcheesi.player-created`)? For now: **no** — keep it simple and revisit with a schema registry ADR.
