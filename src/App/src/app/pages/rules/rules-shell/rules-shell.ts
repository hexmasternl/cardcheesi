import { Component, inject, computed } from '@angular/core';
import { RouterOutlet, RouterLink, RouterLinkActive } from '@angular/router';
import { RulesLanguageService } from '../rules-language.service';

export type { Lang } from '../rules-language.service';

interface Chapter {
  path: string;
  en: string;
  nl: string;
  icon: string;
}

@Component({
  selector: 'app-rules-shell',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  providers: [RulesLanguageService],
  templateUrl: './rules-shell.html',
  styleUrl: './rules-shell.scss',
})
export class RulesShell {
  private readonly langService = inject(RulesLanguageService);
  readonly lang = this.langService.lang;

  readonly chapters: Chapter[] = [
    { path: 'overview',  icon: '📖', en: 'Overview',  nl: 'Overzicht'  },
    { path: 'board',     icon: '🗺️',  en: 'The Board', nl: 'Het Bord'   },
    { path: 'pawns',     icon: '♟',  en: 'Pawns',     nl: 'Pionnen'    },
    { path: 'cards',     icon: '🃏', en: 'Cards',     nl: 'Kaarten'    },
    { path: 'gameplay',  icon: '🎮', en: 'Gameplay',  nl: 'Spelverloop' },
  ];

  readonly heading = computed(() =>
    this.lang() === 'en' ? 'Game Rules' : 'Spelregels'
  );
}
