# Copilot Instructions for CardCheesi

## Architecture

CardCheesi is a digital implementation of a card-based board game (inspired by Parcheesi). The solution is organized under `src/` into three areas:

- **`src/App/CardCheesi/`** — Angular 21 frontend (SPA)
- **`src/Game/`** — .NET 10 backend:
  - `CardCheesi.Game.Abstractions` — shared interfaces/models
  - `CardCheesi.Game` — core game logic
  - `CardCheesi.Game.Api` — ASP.NET Core Web API
  - `CardCheesi.Game.Tests` — xUnit tests
- **`src/Aspire/CardCheesi.Aspire.ServiceDefaults/`** — shared Aspire service defaults (OpenTelemetry, health checks, service discovery)
- **`src/card-cheesi.AppHost/`** — .NET Aspire AppHost that orchestrates all services
- **`src/card-cheesi.slnx`** — solution file

The AppHost is the single entry point to run the full stack locally via the Aspire dashboard.

## Build, Test, and Run

### .NET (run from repo root or solution directory)

```bash
# Build entire solution
dotnet build src/card-cheesi.slnx

# Run all .NET tests
dotnet test src/card-cheesi.slnx

# Run a single test project
dotnet test src/Game/CardCheesi.Game.Tests

# Run a single test by name
dotnet test src/Game/CardCheesi.Game.Tests --filter "FullyQualifiedName~<TestName>"

# Start full stack (Aspire dashboard + all services)
dotnet run --project src/card-cheesi.AppHost
```

### Angular (run from `src/App/CardCheesi/`)

```bash
npm start          # dev server at http://localhost:4200
ng build           # production build
ng test            # run Vitest unit tests
ng test --testFile src/app/app.spec.ts  # run a single test file
```

## Key Conventions

### .NET

- All projects target **`net10.0`** with `<ImplicitUsings>enable</ImplicitUsings>` and `<Nullable>enable</Nullable>`.
- Every service project must call `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` — these are provided by `CardCheesi.Aspire.ServiceDefaults` and wire up OpenTelemetry, health checks, and service discovery.
- Tests use **xUnit**; `<Using Include="Xunit" />` is declared globally in the test project so `[Fact]`/`[Theory]` attributes need no import.

### Angular

- Use **standalone components** (no NgModules). Every component, directive, or pipe declares its own `imports` array.
- Use Angular **Signals** (`signal()`, `computed()`) for reactive state — do not use `BehaviorSubject` or direct property mutation.
- Component styles use **SCSS** (configured globally in `angular.json`).
- PrimeNG is the UI component library. The custom theme is defined in `src/app/theme/card-cheesi.theme.ts` — it extends the **Aura** preset via `definePreset`. Modify that file to change the design token palette. The primary color is `#009ccc`.
- **Dark mode** is toggled by adding/removing the `.dark-mode` CSS class on the document root.
- `providePrimeNG` is configured once in `app.config.ts` — do not call it elsewhere.

## Game Domain

Game rules are documented in `docs/rules/`. Key facts needed for implementing game logic:

- **4 players**, **2 teams** — Team A (P1+P3), Team B (P2+P4). Each player has **4 pawns**.
- **Board**: 64 positions in a loop, plus 4 finish positions per player. Home positions: P1→1, P2→17, P3→33, P4→49.
- **Entering play**: Only an **Ace** or **King** brings a pawn from reserve onto the home position.
- **Card effects**: Ace (enter or +1), King (enter), Four (−4 / reverse), Seven (split across ≤2 pawns), Jack (swap two different-color pawns), Queen (+12); all others move forward by face value.
- **Protection**: A pawn is protected when newly placed at home or when in the finish area. Protected pawns cannot be hit, passed, or swapped by opponents.
- **Winning**: A team wins when both teammates have all 4 pawns in their finish area.

Dealing: 3 rounds per dealer turn — 5 cards, then 4, then 4. Dealer rotates clockwise after all 3 rounds.

## OpenSpec Workflow

Feature changes are managed via the **OpenSpec** CLI (`openspec`) and the skills in `.github/skills/`. Use the Copilot skills instead of the raw CLI for the standard workflow:

| Goal | Skill / Prompt |
|------|---------------|
| Explore / clarify requirements | `openspec-explore` |
| Propose a new change (design + tasks) | `openspec-propose` |
| Implement tasks | `openspec-apply-change` |
| Archive a completed change | `openspec-archive-change` |

Active changes live in `openspec/changes/`; specs in `openspec/specs/`. The schema in use is **`spec-driven`** (see `openspec/config.yaml`).
