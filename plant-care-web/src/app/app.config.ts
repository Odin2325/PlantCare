import {
  inject,
  provideAppInitializer,
} from '@angular/core';
import {
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { routes } from './app.routes';
import { credentialsInterceptor } from './core/auth/interceptors/credentials.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { antiforgeryInterceptor } from './core/security/interceptors/antiforgery.interceptor';

export const appConfig = {
  providers: [
    provideRouter(routes),

    provideHttpClient(
      withInterceptors([
        credentialsInterceptor,
        antiforgeryInterceptor,
      ]),
    ),

    provideAppInitializer(() => {
      const authService = inject(AuthService);

      return firstValueFrom(authService.initialize(),
      );
    }),
  ],
};
