## 1. Install Dependencies

- [ ] 1.1 Run `npm install @ngx-translate/core @ngx-translate/http-loader` from `src/App`

## 2. Translation Resource Files

- [ ] 2.1 Create `src/App/src/assets/i18n/en.json` with all landing-page keys under `landing.*`
- [ ] 2.2 Create `src/App/src/assets/i18n/nl.json` with Dutch translations for all keys in `en.json`

## 3. App Configuration

- [ ] 3.1 Add `provideHttpClient()` to `app.config.ts`
- [ ] 3.2 Add `importProvidersFrom(TranslateModule.forRoot({ loader: { provide: TranslateLoader, useFactory: ..., deps: [HttpClient] } }))` to `app.config.ts`

## 4. Language Service

- [ ] 4.1 Create `src/App/src/app/services/language.service.ts` with `currentLang` signal, `setLanguage(lang)` method, `localStorage` read on init, and `localStorage` write on change
- [ ] 4.2 Write `language.service.spec.ts` covering: default language is `en`, restores from `localStorage`, `setLanguage` updates signal and persists, unrecognised stored value falls back to `en`

## 5. Language Switcher Component

- [ ] 5.1 Create standalone `LanguageSwitcherComponent` in `src/App/src/app/components/language-switcher/` with `EN` / `NL` toggle buttons, `OnPush` change detection, using `LanguageService` via `inject()`
- [ ] 5.2 Style the switcher in `language-switcher.component.scss` using `@use 'variables' as *`
- [ ] 5.3 Add `LanguageSwitcherComponent` to the `app.ts` template and imports array

## 6. Landing Page Migration

- [ ] 6.1 Add `TranslateModule` to `LandingPage` imports array
- [ ] 6.2 Replace all hardcoded strings in `landing.html` with `{{ 'landing.xxx' | translate }}` pipes

## 7. Verification

- [ ] 7.1 Run `ng build` from `src/App` — no errors
- [ ] 7.2 Run `ng test` — all tests pass
