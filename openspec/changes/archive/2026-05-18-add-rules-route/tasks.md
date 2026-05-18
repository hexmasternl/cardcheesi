## 1. Routing

- [x] 1.1 Add `/rules` lazy-loaded route (with redirect to `/rules/overview`) and five child routes to `app.routes.ts`

## 2. Rules Shell

- [x] 2.1 Create `src/app/pages/rules/rules-shell/rules-shell.ts` with `languageSignal = signal<'en'|'nl'>('en')` and chapter navigation data
- [x] 2.2 Create `rules-shell.html` with sidebar chapter links (using `routerLink`/`routerLinkActive`), language toggle buttons, and `<router-outlet>`
- [x] 2.3 Create `rules-shell.scss` with dark sidebar, active link highlight, and responsive layout

## 3. Chapter Components

- [x] 3.1 Create `rules-overview` component with full EN+NL content (game concept, teams, pawns, win condition)
- [x] 3.2 Create `rules-board` component with full EN+NL content (64 positions, home positions table, finish area)
- [x] 3.3 Create `rules-pawns` component with full EN+NL content (entering play, protection, hitting, finish area, swap restrictions)
- [x] 3.4 Create `rules-cards` component with full EN+NL content (all card types and effects)
- [x] 3.5 Create `rules-gameplay` component with full EN+NL content (dealing rounds, turn order, play/discard, teammate pawns)

## 4. Landing Page

- [x] 4.1 Add `RouterLink` import to `LandingPage` component and wire `[routerLink]="['/rules']"` to the "Learn the Rules" button
