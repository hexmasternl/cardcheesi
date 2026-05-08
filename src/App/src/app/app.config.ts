import { ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { providePrimeNG } from 'primeng/config';
import CardCheesiTheme from './theme/card-cheesi.theme';

import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(),
    importProvidersFrom(
      TranslateModule.forRoot({ defaultLanguage: 'en' })
    ),
    ...provideTranslateHttpLoader({ prefix: './i18n/', suffix: '.json' }),
    providePrimeNG({
      theme: {
        preset: CardCheesiTheme,
        options: {
          darkModeSelector: '.dark-mode',
        },
      },
    }),
  ],
};
