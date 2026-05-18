## Purpose

Define the app-shell language switcher UI and persisted language-selection behavior.

## Requirements

### Requirement: Language switcher control in app shell
The app SHALL include a language switcher UI control rendered in the app shell (top-level `app.ts` template). The control SHALL display the current language label and allow the player to toggle between English (`en`) and Dutch (`nl`).

#### Scenario: Switcher shows current language
- **WHEN** the active language is `en`
- **THEN** the switcher control displays `EN` (or equivalent English label)

#### Scenario: Switching to Dutch updates the language
- **WHEN** the player activates the Dutch option in the switcher
- **THEN** `LanguageService.setLanguage('nl')` is called
- **THEN** `currentLang()` returns `'nl'`
- **THEN** all translated text on the current page updates to Dutch without a reload

#### Scenario: Switching to English updates the language
- **WHEN** the player activates the English option in the switcher
- **THEN** `LanguageService.setLanguage('en')` is called
- **THEN** `currentLang()` returns `'en'`
- **THEN** all translated text on the current page updates to English without a reload

### Requirement: Language choice persisted to localStorage
The selected language SHALL be stored under the key `cc-lang` in `localStorage` so it is restored on the next visit.

#### Scenario: Language choice survives page reload
- **WHEN** the player selects `nl` in the switcher
- **THEN** `localStorage.getItem('cc-lang')` returns `'nl'`
- **WHEN** the player reloads the page
- **THEN** the app starts in Dutch without any further interaction

#### Scenario: localStorage updated on every language change
- **WHEN** the player switches language from `nl` back to `en`
- **THEN** `localStorage.getItem('cc-lang')` returns `'en'`
