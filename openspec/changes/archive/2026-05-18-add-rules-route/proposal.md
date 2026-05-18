## Why

Players need an in-app reference for game rules, divided into logical chapters so they can quickly find what they need. Supporting both English and Dutch ensures accessibility for the primary audience.

## What Changes

- Add a `/rules` route to the Angular app.
- Create a `src/pages/rules/` directory with one component per rules chapter.
- Implement bilingual content (English and Dutch) for every rules page.
- Add a rules navigation shell (chapter list / sidebar) so users can jump between chapters.
- Link to the rules from the landing page.

## Capabilities

### New Capabilities

- `rules-pages`: Bilingual (EN/NL) rules section at `/rules` with chapter sub-pages covering overview, board layout, pawns, cards, and gameplay.

### Modified Capabilities

<!-- No existing spec requirements are changing. -->

## Impact

- **Angular routing** (`app.routes.ts`): new `/rules` lazy-loaded route and child chapter routes.
- **New components** under `src/app/pages/rules/`: shell, overview, board, pawns, cards, gameplay pages.
- **i18n / content**: each chapter component ships both English and Dutch text; language toggle is driven by an Angular Signal.
- **Landing page**: add a "Read the Rules" call-to-action button linking to `/rules`.
- No backend or game-logic changes required.
