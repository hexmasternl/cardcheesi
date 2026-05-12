## ADDED Requirements

### Requirement: Integration messages library contains all domain event types
The system SHALL provide a `CardCheesi.IntegrationMessages` class library containing typed integration event records for all significant domain events. All event records SHALL be `sealed record` types with no dependencies on web frameworks, ORMs, or infrastructure packages.

#### Scenario: Library defines player lifecycle events
- **WHEN** the `CardCheesi.IntegrationMessages` project is referenced
- **THEN** the following event records SHALL be available: `PlayerCreatedEvent`, `PlayerWentOfflineEvent`, `PlayerCameOnlineEvent`

#### Scenario: Library defines game lifecycle events
- **WHEN** the `CardCheesi.IntegrationMessages` project is referenced
- **THEN** the following event records SHALL be available: `GameCreatedEvent`, `PlayerAddedToGameEvent`, `PlayerLeftGameEvent`

### Requirement: All integration event records carry a correlation ID and timestamp
Every integration event record SHALL include a `Guid EventId` (unique per event instance) and a `DateTimeOffset OccurredAt` timestamp field.

#### Scenario: Event has identity fields
- **WHEN** an integration event is constructed
- **THEN** it SHALL contain a non-empty `EventId` and a non-default `OccurredAt`

### Requirement: Event records are immutable value types
All integration event records SHALL be `sealed record` types to ensure immutability, structural equality, and clean JSON serialization.

#### Scenario: Event equality is value-based
- **WHEN** two event instances with identical field values are compared
- **THEN** they SHALL be considered equal

### Requirement: Library has no external dependencies
The `CardCheesi.IntegrationMessages` project SHALL reference only the .NET 10 base class library — no NuGet packages for web, Dapr, EF Core, or any other infrastructure framework.

#### Scenario: Library builds without infrastructure packages
- **WHEN** the project is built in isolation
- **THEN** it SHALL compile successfully with zero package references beyond the SDK
