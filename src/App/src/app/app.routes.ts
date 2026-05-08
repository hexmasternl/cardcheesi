import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./pages/landing/landing').then(m => m.LandingPage),
  },
  {
    path: 'rules',
    loadComponent: () =>
      import('./pages/rules/rules-shell/rules-shell').then(m => m.RulesShell),
    children: [
      { path: '', redirectTo: 'overview', pathMatch: 'full' },
      {
        path: 'overview',
        loadComponent: () =>
          import('./pages/rules/rules-overview/rules-overview').then(m => m.RulesOverview),
      },
      {
        path: 'board',
        loadComponent: () =>
          import('./pages/rules/rules-board/rules-board').then(m => m.RulesBoard),
      },
      {
        path: 'pawns',
        loadComponent: () =>
          import('./pages/rules/rules-pawns/rules-pawns').then(m => m.RulesPawns),
      },
      {
        path: 'cards',
        loadComponent: () =>
          import('./pages/rules/rules-cards/rules-cards').then(m => m.RulesCards),
      },
      {
        path: 'gameplay',
        loadComponent: () =>
          import('./pages/rules/rules-gameplay/rules-gameplay').then(m => m.RulesGameplay),
      },
    ],
  },
];
