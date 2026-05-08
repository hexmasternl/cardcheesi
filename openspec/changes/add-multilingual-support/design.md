## Context

The Angular frontend currently has no i18n infrastructure — all text is hardcoded in component TypeScript and templates. CardCheesi targets English and Dutch-speaking players. Adding ngx-translate now enables the in-progress rules pages (and all future pages) to ship fully localised from day one.

The app is Angular 21, standalone-component architecture, using Angular Signals for reactive state. There is no existing HTTP client registration in `app.config.ts`.

## Goals / Non-Goals

**Goals:**
- Wire up `@ngx-translate/core` with `@ngx-translate/http-loader` as the sole translation provider.
- Load translation keys from `src/assets/i18n/{lang}.json` at runtime via HTTP.
- Default language is `en`; supported languages are `en` and `nl`.
- Persist the user's language choice in `localStorage` and restore it on next visit.
- Expose a Signal-based `LanguageService` so any component can read and switch locale reactively.
- Add a minimal language switcher control to the app shell.
- Migrate existing landing-page strings to translation keys as a smoke-test.

**Non-Goals:**
- Right-to-left layout support.
- More than two languages (en / nl) in this change.
- Server-side rendering / SSR i18n.
- Automatic locale detection from browser `Accept-Language` (keep it simple: explicit user choice only, falling back to `en`).

## Decisions

### 1. ngx-translate over Angular built-in i18n (`@angular/localize`)

**Decision**: Use `@ngx-translate/core`.

**Rationale**: Angular's built-in i18n requires a separate build per locale and does not support runtime language switching — a mandatory UX requirement for this app. ngx-translate loads translations at runtime via HTTP and allows switching without a page reload.

**Alternatives considered**: `transloco` — equally capable, but ngx-translate has broader adoption and is the user-specified library.

---

### 2. HTTP loader (`@ngx-translate/http-loader`) over bundled translations

**Decision**: Load `src/assets/i18n/{lang}.json` via HTTP at app startup.

**Rationale**: Keeps translation files outside the JavaScript bundle, allowing them to be updated without rebuilding the app. Aligns with the user's explicit requirement.

**Consequence**: `provideHttpClient()` must be registered in `app.config.ts`.

---

### 3. Signal-based `LanguageService`

**Decision**: Wrap ngx-translate's `TranslateService` in a dedicated `LanguageService` that exposes a `currentLang` signal and a `setLanguage(lang)` method.

**Rationale**: Keeps the rest of the app decoupled from ngx-translate's RxJS-based API; fits the project's Signals-first convention. Components observe `currentLang` reactively without subscribing to observables.

---

### 4. `localStorage` persistence key: `cc-lang`

**Decision**: Store the selected language under the key `cc-lang`.

**Rationale**: Short, app-prefixed, unlikely to clash with third-party scripts. On init, `LanguageService` reads this key; if absent or unrecognised it falls back to `en`.

---

### 5. Translation key namespace convention: `<page>.<section>.<key>`

**Decision**: Nest translation keys by page and section, e.g. `landing.hero.title`, `rules.cards.ace`.

**Rationale**: Flat keys become unmanageable as the app grows. Namespacing by page/section keeps `en.json` / `nl.json` readable and maps cleanly to the component hierarchy.

## Risks / Trade-offs

- **HTTP latency on first load** → Mitigation: translation files are small JSON blobs served from the same origin; Angular's built-in HTTP caching applies. Show a minimal loading state via `APP_INITIALIZER` if needed.
- **Key drift** (keys exist in `en.json` but not `nl.json`) → Mitigation: ngx-translate falls back to the default language (`en`) for missing keys, so the app never shows blank strings. A lint/CI check can be added later.
- **`TranslateModule` imported in every standalone component** → Mitigation: re-export `TranslateModule` from a shared barrel or import it once at the root via `importProvidersFrom`; components use the `translate` pipe directly.

## Migration Plan

1. Install packages (`npm install @ngx-translate/core @ngx-translate/http-loader`).
2. Register providers in `app.config.ts`.
3. Create `src/assets/i18n/en.json` and `nl.json` with landing-page keys.
4. Implement `LanguageService`.
5. Add language switcher component to app shell (`app.ts` template).
6. Migrate `LandingPage` strings to use the `translate` pipe.
7. Verify both languages render correctly via `ng serve`.

Rollback: removing the three provider lines from `app.config.ts` and reverting the landing component returns the app to its previous state without touching any other file.

## Open Questions

- Should the language switcher be a flag icon, a text label (EN / NL), or a `p-select` dropdown? *(Suggest: simple text toggle button for now; can be styled later.)*
