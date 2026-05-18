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
- **`src/Aspire/CardCheesi.Aspire.AppHost/`** — .NET Aspire AppHost that orchestrates all services
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
dotnet run --project src/Aspire/CardCheesi.Aspire.AppHost
```

### Aspire: Applying Backend Changes at Runtime

When the Aspire AppHost is already running, **do not stop it to build the backend**. Instead, use the Aspire MCP `execute_resource_command` tool with command `rebuild` on the affected resource. This compiles the project and restarts the resource in-place without disrupting other services.

**Rule**: After modifying any .NET backend file (any project under `src/Game/` or `src/Aspire/`), check whether the Aspire AppHost is running via `aspire-list_resources`. If it is running, trigger a rebuild of the affected resource immediately:

```
aspire-execute_resource_command(resourceName: "<resource-name>", commandName: "rebuild")
```

The resource name for the API is typically `api-*` (check `aspire-list_resources` for the exact name). Wait for the rebuild to confirm `Build succeeded` before considering the change deployed.

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

#### C# Code Guidelines

All C# code **must comply with the [HexMaster Code Guidelines MCP](hexmaster-design-guidelines)**. Before writing or reviewing C# code, consult the guidelines via the `hexmaster-design-guidelines` MCP tools:

- `list_docs` — browse all available guidelines, ADRs, and recommendations
- `get_doc` — retrieve a specific guideline by ID

Key guidelines in effect for this project:

| Guideline | ID |
|-----------|-----|
| Adopt .NET 10 as target framework | `0001-adopt-dotnet-10` |
| Modular monolith project structure | `0002-modular-monolith-structure` |
| Use .NET Aspire for ASP.NET services | `0003-recommend-aspire-for-aspnet-projects` |
| CQRS for ASP.NET API projects | `0004-cqrs-recommendation-for-aspnet-api` |
| Minimal APIs over controllers | `0005-minimal-apis-over-controllers` |
| Vertical Slice Architecture | `0007-vertical-slice-architecture` |
| OpenTelemetry for observability | `0008-adopt-opentelemetry-for-observability` |
| Unit testing with xUnit, Moq, and Bogus | `unit-testing-xunit-moq-bogus` |

#### Code Coverage

All C# code must maintain **≥ 80% unit test coverage** for Core and Server projects. This is enforced via `coverlet.collector`. When adding or modifying C# code, ensure the coverage threshold is met:

```bash
# Collect coverage and verify threshold
dotnet test src/card-cheesi.slnx --collect:"XPlat Code Coverage" \
  /p:Threshold=80 /p:ThresholdType=line /p:ThresholdStat=total
```

- Use **Moq** for mocking interfaces and collaborators.
- Use **Bogus** to generate realistic test data; use deterministic seeds when asserting on generated values.
- Encapsulate test object creation in factory classes under `Tests/Factories/`.

### Angular

The frontend lives in `src/App/CardCheesi/` and is an **Angular 21** SPA.

#### Core Patterns

- Use **standalone components** (no NgModules). Every component, directive, and pipe declares its own `imports` array.
- Use Angular **Signals** (`signal()`, `computed()`, `effect()`) for all reactive state. Do **not** use `BehaviorSubject`, `Subject`, or direct property mutation.
- Use `input()` / `output()` signal-based APIs instead of `@Input()` / `@Output()` decorators.
- Use `inject()` for dependency injection inside functions and constructors — avoid constructor parameter injection for new code.
- Lazy-load feature routes; keep the root bundle lean.
- Use `OnPush` change detection strategy on all components.

#### Styling

- Component styles are **SCSS only** — no plain CSS files (configured via `angular.json`).
- Follow [ADR 0006](hexmaster-design-guidelines: `0006-centralized-frontend-styling-variables`): **never hardcode** color hex values, font names, or size values directly in component styles. All design tokens are centralized in `src/styles/_variables.scss`.
- Import variables in component stylesheets with `@use 'variables' as *;`.
- Dark mode is toggled by adding/removing the `.dark-mode` CSS class on the document root — never check `prefers-color-scheme` directly in components.

#### PrimeNG

- **PrimeNG 21** is the UI component library (`primeng`, `@primeng/themes`).
- The custom theme is defined in `src/app/theme/card-cheesi.theme.ts` — it extends the **Aura** preset via `definePreset`. Primary color is `#009ccc`. Modify only this file to change palette tokens.
- `providePrimeNG` is configured **once** in `app.config.ts` — do not call it elsewhere.
- Import individual PrimeNG components into each standalone component's `imports` array (e.g., `ButtonModule`, `CardModule`).
- Use PrimeNG design tokens for spacing, color, and typography inside theme overrides — do not override PrimeNG component CSS directly.
- PrimeIcons are available globally via `primeicons` — use them with `<i class="pi pi-*">`.

#### Testing (Angular)

- Test framework is **Vitest** (configured in `angular.json`).
- Run tests with `ng test` from `src/App/CardCheesi/`.
- Use Angular's `TestBed` for component tests; prefer `ComponentFixture` with signal-aware change detection.
- Mock services via `TestBed.overrideProvider` or `provideValue`.
- Test file convention: `*.spec.ts` co-located with the component/service being tested.

---

### Babylon.js & 3D Game Board

The 3D game board is rendered using **Babylon.js** with the **WebGPU** backend (falling back to WebGL 2). Babylon.js is a high-level 3D engine — it runs *on top of* WebGPU/WebGL, not instead of it.

#### Engine Setup

Always initialize the engine with `WebGPUEngine.CreateAsync()` — it auto-detects browser support and falls back gracefully:

```typescript
import { WebGPUEngine, Engine, Scene } from '@babylonjs/core';

async function createEngine(canvas: HTMLCanvasElement): Promise<Engine> {
  if (await WebGPUEngine.IsSupportedAsync) {
    const engine = new WebGPUEngine(canvas);
    await engine.initAsync();
    return engine;
  }
  return new Engine(canvas, true); // WebGL 2 fallback
}
```

- Always call `engine.runRenderLoop(() => scene.render())` to start the loop.
- Always call `engine.resize()` on `window.resize`.
- Dispose the engine and scene when the Angular component is destroyed — use `DestroyRef` and `inject(DestroyRef).onDestroy(...)`.

#### Angular Integration

Wrap the Babylon.js canvas in a dedicated standalone component (`GameBoardComponent`). Use `afterNextRender` to initialize the engine after the DOM is ready:

```typescript
@Component({
  selector: 'app-game-board',
  template: `<canvas #gameCanvas style="width:100%;height:100%"></canvas>`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GameBoardComponent {
  private readonly canvasRef = viewChild.required<ElementRef<HTMLCanvasElement>>('gameCanvas');
  private engine?: Engine;

  constructor() {
    afterNextRender(async () => {
      this.engine = await createEngine(this.canvasRef().nativeElement);
      // build scene ...
    });
    inject(DestroyRef).onDestroy(() => this.engine?.dispose());
  }
}
```

#### 3D Asset Format

All 3D models and environments **must use glTF 2.0 (`.glb` binary format)**:

- Self-contained binary (geometry + textures + materials in one file).
- PBR materials supported natively by Babylon.js.
- Load with `SceneLoader.ImportMeshAsync`:

```typescript
import { SceneLoader } from '@babylonjs/core';
import '@babylonjs/loaders/glTF'; // register the glTF loader

const { meshes } = await SceneLoader.ImportMeshAsync('', 'assets/models/', 'board.glb', scene);
```

- Store 3D assets in `src/assets/models/`.
- Prefer Draco-compressed `.glb` files for production to reduce download size.

#### Scene & Board Conventions

- One `Scene` per game session — do not create multiple scenes.
- Use an **`ArcRotateCamera`** to allow players to orbit the board freely. Set `lowerRadiusLimit` and `upperRadiusLimit` to keep the board in frame.
- Light the scene with a `HemisphericLight` (ambient) and a `DirectionalLight` (shadow casting).
- Represent board positions as named `TransformNode` anchors in the glTF model — resolve them by name at runtime: `scene.getTransformNodeByName('position_01')`.
- Pawns are `Mesh` instances cloned from a master pawn mesh; color them via `PBRMaterial.albedoColor`.
- Game state drives the 3D scene through Angular Signals — use `effect(() => { /* update mesh positions from signal */ })`. Never mutate game state from inside the render loop.

#### Performance

- Enable hardware scaling: `engine.setHardwareScalingLevel(1 / window.devicePixelRatio)`.
- Call `mesh.freezeWorldMatrix()` on static board geometry.
- Batch small static meshes with `Mesh.MergeMeshes()` at load time.
- Dispose unused textures and meshes explicitly — do not rely on GC.
- Prefer **`PBRMaterial`** over legacy `StandardMaterial` for consistent lighting.

#### WebGPU Notes

- WebGPU is available in Chrome 113+, Edge 113+, and recent Firefox Nightly. Always provide the WebGL 2 fallback (see Engine Setup above).
- WebGPU compute shaders are available via `ComputeShader` in Babylon.js — use for GPU-side effects (particles, board animations).
- Do **not** write raw WGSL shader code unless absolutely necessary — prefer Babylon.js **NodeMaterial** for custom shaders.

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

## Git Commit Convention

After completing each task or logical unit of work, **always commit the changed files** using Git with a clear, descriptive commit message.

### Commit message format

```
<type>(<scope>): <short imperative summary>

<optional body — what changed and why, wrapped at 72 chars>

Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>
```

**Types**: `feat`, `fix`, `refactor`, `test`, `docs`, `chore`, `style`, `perf`  
**Scope**: the area of the codebase affected (e.g., `api`, `frontend`, `apphost`, `tests`, `openspec`)

### Rules

- Stage only the files relevant to the completed task (`git add <files>` — avoid blanket `git add .` unless all changes belong to the same logical unit).
- Always include the `Co-authored-by` trailer exactly as shown above.
- Keep the subject line ≤ 72 characters and in the imperative mood ("Add player registration" not "Added" or "Adding").
- If the task closes a spec requirement or task item, mention it in the body (e.g., `Implements task 4.3 of create-new-game`).
- Do **not** commit secrets, build artefacts (`bin/`, `obj/`, `node_modules/`, `dist/`), or migration snapshots that were not explicitly modified as part of the task.
