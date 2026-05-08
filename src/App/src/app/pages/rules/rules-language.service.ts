import { Injectable, signal } from '@angular/core';

export type Lang = 'en' | 'nl';

@Injectable()
export class RulesLanguageService {
  readonly lang = signal<Lang>('en');
}
