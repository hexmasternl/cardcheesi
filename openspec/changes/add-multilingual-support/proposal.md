## Why

CardCheesi needs to serve players in multiple languages — English and Dutch — and currently has no translation infrastructure. Adding ngx-translate now lays the foundation for all future multilingual content, including the in-progress rules pages.

## What Changes

- Install `@ngx-translate/core` and `@ngx-translate/http-loader`.
- Configure `TranslateModule` with the HTTP loader in `app.config.ts`, loading JSON files from `src/assets/i18n/`.
- Add translation resource files: `en.json` (default) and `nl.json`.
- Expose a `LanguageService` (Signal-based) so components can read and switch the active language.
- Add a language switcher UI element (e.g., a flag/label toggle in the app shell or header).
- Migrate existing static text in the landing page to use translation keys.

## Capabilities

### New Capabilities

- `multilingual-core`: ngx-translate wired up with HTTP loader, `en.json` / `nl.json` resource files, English as default, and a `LanguageService` for reading/switching locale via Signals.
- `language-switcher`: UI control that lets players toggle between English and Dutch, persisting the choice to `localStorage`.

### Modified Capabilities

<!-- No existing spec requirements are changing. -->

## Impact

- **`package.json`**: new runtime dependencies `@ngx-translate/core`, `@ngx-translate/http-loader`.
- **`app.config.ts`**: add `provideHttpClient()` (required by the HTTP loader) and `importProvidersFrom(TranslateModule.forRoot(...))`.
- **`src/assets/i18n/`**: new directory with `en.json` and `nl.json` translation files.
- **`app.ts` / app shell**: language switcher component added to the top-level template.
- **`pages/landing/`**: static strings replaced with `translate` pipe / `TranslateService`.
- No backend or game-logic changes required.
