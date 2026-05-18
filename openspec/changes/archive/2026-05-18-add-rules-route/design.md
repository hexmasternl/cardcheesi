## Context

The Angular app has a landing page at `/` but no in-app rules reference. All game rules documentation lives in `docs/rules/` as Markdown files covering overview, board layout, pawns, cards, and gameplay. Players need an accessible, navigable rules section within the app without leaving to read raw docs.

The app uses Angular 21 standalone components, signals for state, PrimeNG (Aura theme), Quicksand/Luckiest Guy fonts, and the established primary/secondary colour palette.

## Goals / Non-Goals

**Goals:**
- Add a `/rules` lazy-loaded route with a rules shell component and five chapter sub-pages
- Provide bilingual content (English and Dutch) for all chapter pages, toggled via an Angular Signal
- Add a sidebar/chapter navigation so users can jump between chapters
- Wire the "Learn the Rules" button on the landing page to `/rules`
- Match the visual style established by the landing page (same fonts, colours, PrimeNG components)

**Non-Goals:**
- No backend changes or API calls
- No persistent language preference (in-memory signal per session is sufficient)
- No search or full-text indexing of rules
- No print or export functionality

## Decisions

### 1. Single shell component with router child outlet
The `/rules` route renders a `RulesShellComponent` that owns the sidebar and a `<router-outlet>` for chapter content. Each chapter is a separate lazy-loaded child route.

**Rationale:** Keeps the sidebar persistent across chapter navigation without re-rendering, and each chapter chunk loads on demand. Alternative (single component with `@switch`) was rejected because it cannot be deep-linked.

### 2. Language toggle via Angular Signal
A `languageSignal = signal<'en' | 'nl'>('en')` is defined once in `RulesShellComponent` and passed into each chapter via an `@Input`. Chapter components expose both language strings in a `content` computed property and render the active one.

**Rationale:** Lightweight, no NgRx or HTTP overhead. Keeps i18n self-contained to the rules section. Alternative (Angular i18n build-time) was rejected — too heavy for a bilingual toggle within a single section.

### 3. Chapter structure mirrors docs/rules/
Five chapters map 1-to-1 to the existing docs:

| Route | Component | Source |
|---|---|---|
| `/rules` (redirect) | — | → `/rules/overview` |
| `/rules/overview` | `RulesOverviewComponent` | `docs/rules/overview.md` |
| `/rules/board` | `RulesBoardComponent` | `docs/rules/board.md` |
| `/rules/pawns` | `RulesPawnsComponent` | `docs/rules/pawns.md` |
| `/rules/cards` | `RulesCardsComponent` | `docs/rules/cards.md` |
| `/rules/gameplay` | `RulesGameplayComponent` | `docs/rules/gameplay.md` |

### 4. Visual style
- Shell uses the dark sidebar pattern (dark navy `#0b1e3a` background, primary-coloured active link)
- Chapter content area uses a light background consistent with the landing page feature sections
- PrimeNG `p-button` components used for language toggle

## Risks / Trade-offs

- [Risk] Inline bilingual strings make components verbose → Mitigation: content objects are defined at the top of each component, keeping templates clean
- [Risk] Deep-linking directly to `/rules/cards` must work → Mitigation: each chapter is a named child route; redirect from `/rules` to `/rules/overview` handles the bare path

## Open Questions

None — all decisions resolved above.
