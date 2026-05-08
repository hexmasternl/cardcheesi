## ADDED Requirements

### Requirement: ngx-translate wired with HTTP loader
The app SHALL configure `@ngx-translate/core` with `@ngx-translate/http-loader` as the sole translation provider. Translation files SHALL be loaded from `src/assets/i18n/{lang}.json` via HTTP. `provideHttpClient()` SHALL be registered in `app.config.ts`.

#### Scenario: English translation file loads on startup
- **WHEN** the application starts with no language stored in `localStorage`
- **THEN** `en.json` is fetched from `assets/i18n/en.json`
- **THEN** all translation keys from `en.json` are available via the `translate` pipe

#### Scenario: Dutch translation file loads when language is Dutch
- **WHEN** the active language is set to `nl`
- **THEN** `nl.json` is fetched from `assets/i18n/nl.json`
- **THEN** all translation keys from `nl.json` are available via the `translate` pipe

---

### Requirement: English is the default language
The app SHALL default to English (`en`) when no language preference is stored in `localStorage` or when the stored value is unrecognised.

#### Scenario: No stored preference defaults to English
- **WHEN** `localStorage` does not contain the key `cc-lang`
- **THEN** the active language is `en`

#### Scenario: Unrecognised stored preference falls back to English
- **WHEN** `localStorage` contains `cc-lang` with an unrecognised value (e.g. `fr`)
- **THEN** the active language falls back to `en`

---

### Requirement: LanguageService exposes current language as a Signal
The app SHALL provide a `LanguageService` with a `currentLang` Signal of type `string` and a `setLanguage(lang: string): void` method. The signal SHALL reflect the active ngx-translate language at all times.

#### Scenario: currentLang signal reflects active language
- **WHEN** `setLanguage('nl')` is called on `LanguageService`
- **THEN** `currentLang()` returns `'nl'`
- **THEN** the ngx-translate `TranslateService` active language is `'nl'`

#### Scenario: LanguageService initialises from localStorage
- **WHEN** `localStorage` contains `cc-lang = 'nl'` at app startup
- **THEN** `currentLang()` returns `'nl'` immediately after the service is constructed

---

### Requirement: Translation key namespace convention
Translation keys SHALL follow the convention `<page>.<section>.<key>` (e.g. `landing.hero.title`, `landing.features.cards.title`). Both `en.json` and `nl.json` SHALL contain identical key structures; values differ by language.

#### Scenario: Missing key falls back to English
- **WHEN** a translation key exists in `en.json` but is absent from `nl.json`
- **THEN** the `translate` pipe renders the English value instead of an empty string

---

### Requirement: Landing page uses translation keys
All user-visible static strings in `LandingPage` SHALL be migrated to use the `translate` pipe with namespaced keys under `landing.*`.

#### Scenario: Landing page renders in English by default
- **WHEN** the app loads with no stored language preference
- **THEN** the landing page displays English text for all translated strings

#### Scenario: Landing page switches to Dutch without reload
- **WHEN** `setLanguage('nl')` is called while the landing page is visible
- **THEN** all translated strings on the landing page update to Dutch without a page reload
