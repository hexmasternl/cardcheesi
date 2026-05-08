import { Injectable, inject, signal } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';

const SUPPORTED_LANGS = ['en', 'nl'] as const;
const STORAGE_KEY = 'cc-lang';
const DEFAULT_LANG = 'en';

@Injectable({ providedIn: 'root' })
export class LanguageService {
  private readonly translate = inject(TranslateService);

  readonly currentLang = signal<string>(DEFAULT_LANG);

  constructor() {
    const stored = localStorage.getItem(STORAGE_KEY);
    const initial = SUPPORTED_LANGS.includes(stored as (typeof SUPPORTED_LANGS)[number])
      ? (stored as string)
      : DEFAULT_LANG;

    this.translate.addLangs([...SUPPORTED_LANGS]);
    this.translate.setDefaultLang(DEFAULT_LANG);
    this.translate.use(initial);
    this.currentLang.set(initial);
  }

  setLanguage(lang: string): void {
    const resolved = SUPPORTED_LANGS.includes(lang as (typeof SUPPORTED_LANGS)[number])
      ? lang
      : DEFAULT_LANG;

    this.translate.use(resolved);
    this.currentLang.set(resolved);
    localStorage.setItem(STORAGE_KEY, resolved);
  }
}
