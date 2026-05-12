import { APP_INITIALIZER, ApplicationConfig, importProvidersFrom, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { providePrimeNG } from 'primeng/config';
import CardCheesiTheme from './theme/card-cheesi.theme';

import { routes } from './app.routes';
import { AuthService } from './services/auth.service';
import { authInterceptor } from './interceptors/auth.interceptor';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimationsAsync(),
    provideHttpClient(withInterceptors([authInterceptor])),
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
    {
      provide: APP_INITIALIZER,
      useFactory: (authService: AuthService) => () => authService.tryRestoreSession(),
      deps: [AuthService],
      multi: true,
    },
  ],
};
