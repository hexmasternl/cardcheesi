import { TestBed } from '@angular/core/testing';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { LanguageService } from './language.service';

describe('LanguageService', () => {
  let service: LanguageService;
  let translateService: TranslateService;

  function setup(storedLang?: string) {
    localStorage.clear();
    if (storedLang !== undefined) {
      localStorage.setItem('cc-lang', storedLang);
    }

    TestBed.resetTestingModule();
    TestBed.configureTestingModule({
      imports: [TranslateModule.forRoot()],
    });

    service = TestBed.inject(LanguageService);
    translateService = TestBed.inject(TranslateService);
  }

  afterEach(() => localStorage.clear());

  it('defaults to English when no stored preference', () => {
    setup();
    expect(service.currentLang()).toBe('en');
    expect(translateService.currentLang).toBe('en');
  });

  it('restores Dutch from localStorage', () => {
    setup('nl');
    expect(service.currentLang()).toBe('nl');
    expect(translateService.currentLang).toBe('nl');
  });

  it('falls back to English for unrecognised stored value', () => {
    setup('fr');
    expect(service.currentLang()).toBe('en');
    expect(translateService.currentLang).toBe('en');
  });

  it('setLanguage updates signal and calls TranslateService', () => {
    setup();
    service.setLanguage('nl');
    expect(service.currentLang()).toBe('nl');
    expect(translateService.currentLang).toBe('nl');
  });

  it('setLanguage persists choice to localStorage', () => {
    setup();
    service.setLanguage('nl');
    expect(localStorage.getItem('cc-lang')).toBe('nl');
  });

  it('setLanguage switching back to English updates localStorage', () => {
    setup('nl');
    service.setLanguage('en');
    expect(localStorage.getItem('cc-lang')).toBe('en');
    expect(service.currentLang()).toBe('en');
  });

  it('setLanguage with unsupported lang falls back to English', () => {
    setup();
    service.setLanguage('de');
    expect(service.currentLang()).toBe('en');
    expect(localStorage.getItem('cc-lang')).toBe('en');
  });
});
